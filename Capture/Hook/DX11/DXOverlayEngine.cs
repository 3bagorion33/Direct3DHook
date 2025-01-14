﻿using Capture.Hook.Common;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;

namespace Capture.Hook.DX11
{
    internal class DXOverlayEngine : Component
    {
        public List<IOverlay> Overlays { get; set; }
        public bool DeferredContext => _deviceContext.TypeInfo == DeviceContextType.Deferred;

        private bool _initialised = false;
        private bool _initialising = false;
        private Device _device;
        private DeviceContext _deviceContext;
        private Texture2D _renderTarget;
        private RenderTargetView _renderTargetView;
        private DXSprite _spriteEngine;
        private Dictionary<string, DXFont> _fontCache = new Dictionary<string, DXFont>();
        private Dictionary<Element, DXImage> _imageCache = new Dictionary<Element, DXImage>();

        public DXOverlayEngine()
        {
            Overlays = new List<IOverlay>();
        }

        private void EnsureInitiliased()
        {
            if (!_initialised)
                throw new InvalidOperationException("DXOverlayEngine must be initialised.");
        }

        public bool Initialise(SharpDX.DXGI.SwapChain swapChain)
        {
            return Initialise(swapChain.GetDevice<Device>(), swapChain.GetBackBuffer<Texture2D>(0));
        }

        public bool Initialise(Device device, Texture2D renderTarget)
        {
            if (_initialising)
                return false;

            _initialising = true;

            try
            {

                _device = device;
                _renderTarget = renderTarget;
                try
                {
                    _deviceContext = ToDispose(new DeviceContext(_device));
                }
                catch (SharpDXException)
                {
                    _deviceContext = _device.ImmediateContext;
                }

                _renderTargetView = ToDispose(new RenderTargetView(_device, _renderTarget));

                _spriteEngine = ToDispose(new DXSprite(_device, _deviceContext));
                if (!_spriteEngine.Initialize())
                    return false;

                // Initialise any resources required for overlay elements
                InitialiseElementResources();

                _initialised = true;
                return true;
            }
            finally
            {
                _initialising = false;
            }
        }

        private void InitialiseElementResources()
        {
            foreach (var overlay in Overlays)
            {
                foreach (var element in overlay.Elements)
                {
                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        GetFontForTextElement(textElement);
                    }
                    else if (imageElement != null)
                    {
                        GetImageForImageElement(imageElement);
                    }
                }
            }
        }

        private void Begin()
        {
            SharpDX.Mathematics.Interop.RawViewportF[] viewportf = { new ViewportF(0, 0, _renderTarget.Description.Width, _renderTarget.Description.Height, 0, 1) };
            _deviceContext.Rasterizer.SetViewports(viewportf);
            _deviceContext.OutputMerger.SetTargets(_renderTargetView);
        }

        /// <summary>
        /// Draw the overlay(s)
        /// </summary>
        public void Draw()
        {
            EnsureInitiliased();

            Begin();

            foreach (var overlay in Overlays)
            {
                if (overlay.Hidden)
                    continue;

                foreach (var element in overlay.Elements)
                {
                    if (element.Hidden)
                        continue;

                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        DXFont font = GetFontForTextElement(textElement);
                        if (font != null && !string.IsNullOrEmpty(textElement.Text))
                            _spriteEngine.DrawString(textElement.Location.X, textElement.Location.Y, textElement.Text, textElement.Color, font);
                    }
                    else if (imageElement != null)
                    {
                        DXImage image = GetImageForImageElement(imageElement);
                        if (image != null)
                            _spriteEngine.DrawImage(imageElement.Location.X, imageElement.Location.Y, imageElement.Scale, imageElement.Angle, imageElement.Tint, image);
                    }
                }
            }

            End();
        }

        private void End()
        {
            if (DeferredContext)
            {
                var commandList = _deviceContext.FinishCommandList(true);
                _device.ImmediateContext.ExecuteCommandList(commandList, true);
                commandList.Dispose();
            }
        }

        private DXFont GetFontForTextElement(TextElement element)
        {
            DXFont result = null;

            string fontKey = string.Format("{0}{1}{2}{3}", element.Font.Name, element.Font.Size, element.Font.Style, element.AntiAliased);

            if (!_fontCache.TryGetValue(fontKey, out result))
            {
                result = ToDispose(new DXFont(_device, _deviceContext));
                result.Initialize(element.Font.Name, element.Font.Size, element.Font.Style, element.AntiAliased);
                _fontCache[fontKey] = result;
            }
            return result;
        }

        private DXImage GetImageForImageElement(ImageElement element)
        {
            DXImage result = null;

            if (!_imageCache.TryGetValue(element, out result))
            {
                result = ToDispose(new DXImage(_device, _deviceContext));
                result.Initialise(element.Bitmap);
                _imageCache[element] = result;
            }

            return result;
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">true if disposing both unmanaged and managed</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) // was true
            {
                _device?.Dispose();
                _device = null;
            }
            base.Dispose(disposing);
        }
    }
}
