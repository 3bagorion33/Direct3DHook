using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Capture.Hook.DX11
{
    public class ScreenAlignedQuadRenderer : RendererBase
    {
        private string shaderCodeVertexIn = @"Texture2D<float4> Texture0 : register(t0);
SamplerState Sampler : register(s0);

struct VertexIn
{
    float4 Position : SV_Position;// Position - xyzw
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

// Screen-Aligned Quad: vertex shader main function
PixelIn VSMain(VertexIn vertex)
{
    PixelIn result = (PixelIn)0;
    
    // The input quad is expected in device coordinates 
    // (i.e. 0,0 is center of screen, -1,1 top left, 1,-1 bottom right)
    // Therefore no transformation!
    result.Position = vertex.Position;
    result.Position.w = 1.0f;

    // The UV coordinates are top-left 0,0 bottom-right 1,1
    result.UV.x = result.Position.x * 0.5 + 0.5;
    result.UV.y = result.Position.y * -0.5 + 0.5;

    return result;
}

float4 PSMain(PixelIn input) : SV_Target
{
    return Texture0.Sample(Sampler, input.UV);
}
";
        private string shaderCode = @"Texture2D<float4> Texture0 : register(t0);
SamplerState Sampler : register(s0);

struct PixelIn
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
};

// Vertex shader outputs a full screen quad with UV coords without vertex buffer
PixelIn VSMain(uint vertexId: SV_VertexID)
{
    PixelIn result = (PixelIn)0;
    
    // The input quad is expected in device coordinates 
    // (i.e. 0,0 is center of screen, -1,1 top left, 1,-1 bottom right)
    // Therefore no transformation!

    // The UV coordinates are top-left 0,0, bottom-right 1,1
    result.UV = float2((vertexId << 1) & 2, vertexId & 2 );
    result.Position = float4( result.UV * float2( 2.0f, -2.0f ) + float2( -1.0f, 1.0f), 0.0f, 1.0f );

    return result;
}

float4 PSMain(PixelIn input) : SV_Target
{
    return Texture0.Sample(Sampler, input.UV);
}
";

        // The vertex shader
        private VertexShader vertexShader;

        // The pixel shader
        private PixelShader pixelShader;
        private SamplerState pointSamplerState;
        private SamplerState linearSampleState;

        // The vertex layout for the IA
        private InputLayout vertexLayout;

        // The vertex buffer
        private Buffer vertexBuffer;

        // The vertex buffer binding
        private VertexBufferBinding vertexBinding;

        public bool UseLinearSampling { get; set; }
        public ShaderResourceView ShaderResource { get; set; }
        public RenderTargetView RenderTargetView { get; set; }
        public Texture2D RenderTarget { get; set; }

        public ScreenAlignedQuadRenderer() { }

        /// <summary>
        /// Create any device dependent resources here.
        /// This method will be called when the device is first
        /// initialized or recreated after being removed or reset.
        /// </summary>
        protected override void CreateDeviceDependentResources()
        {
            // Ensure that if already set the device resources
            // are correctly disposed of before recreating
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref pixelShader);
            RemoveAndDispose(ref pointSamplerState);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            // Get a reference to the Device1 instance and immediate context
            var device = DeviceManager.Direct3DDevice;
            var context = DeviceManager.Direct3DContext;

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif

            // Compile and create the vertex shader
            using (var vertexShaderBytecode = ToDispose(ShaderBytecode.Compile(shaderCode, "VSMain", "vs_4_0", shaderFlags, EffectFlags.None, null, null)))
            {
                vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));
            }

            // Compile and create the pixel shader
            using (var bytecode = ToDispose(ShaderBytecode.Compile(shaderCode, "PSMain", "ps_5_0", shaderFlags, EffectFlags.None, null, null)))
                pixelShader = ToDispose(new PixelShader(device, bytecode));

            linearSampleState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            }));

            pointSamplerState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            }));

            context.Rasterizer.State = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
            }));
        }

        protected override void DoRender()
        {
            var context = DeviceManager.Direct3DContext;

            context.ClearRenderTargetView(RenderTargetView, Color.CornflowerBlue);

            // Set sampler
            SharpDX.Mathematics.Interop.RawViewportF[] viewportf = { new ViewportF(0, 0, RenderTarget.Description.Width, RenderTarget.Description.Height, 0, 1) };
            context.Rasterizer.SetViewports(viewportf);
            context.PixelShader.SetSampler(0, UseLinearSampling ? linearSampleState : pointSamplerState);

            // Set shader resource
            //bool isMultisampledSRV = false;
            if (ShaderResource != null && !ShaderResource.IsDisposed)
            {
                context.PixelShader.SetShaderResource(0, ShaderResource);
            }

            // Set pixel shader
            context.PixelShader.Set(pixelShader);

            // Set vertex shader
            context.VertexShader.Set(vertexShader);

            // Update vertex layout to use
            context.InputAssembler.InputLayout = null;

            // Tell the IA we are using a triangle strip
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;

            // Set the render target
            context.OutputMerger.SetTargets(RenderTargetView);

            // Draw the 4 vertices that make up the triangle strip
            context.Draw(4, 0);

            // Remove the render target from the pipeline so that we can read from it if necessary
            context.OutputMerger.SetTargets((RenderTargetView) null);
        }
    }
}
