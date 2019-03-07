namespace NyuuGames.Uprising.Game.Systems
{
    using System;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Components;
    using Mugen.ECS;
    using Veldrid;
    using Veldrid.ImageSharp;
    using Veldrid.Sdl2;
    using Veldrid.SPIRV;

    public sealed class DrawSprites : IDisposable
    {
        private readonly EntityManager _entityManager;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Sdl2Window _window;
        private readonly QueryResult _landLayerResults;
        private readonly QueryResult _propLayerResults;
        private readonly QueryResult _cursourLayerResults;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private ResourceLayout _texLayout;
        private Shader[] _shaders;
        private Pipeline _pipeline;
        private CommandList _commandList;
        private DeviceBuffer _orthoBuffer;
        private ResourceLayout _orthoLayout;
        private ResourceSet _orthoSet;
        private DeviceBuffer _modelBuffer;
        private ResourceSet _texSet;
        private ResourceLayout _modelLayout;
        private ResourceSet _modelSet;

        private readonly (Vector2, Vector2)[] _vertexArray = new (Vector2, Vector2)[1024];
        private readonly Matrix4x4[] _modelArray = new Matrix4x4[1024];

        private const string _vertexCode = @"
#version 450

const vec4 positions[4] = vec4[](
    vec4(-0.5, 0.5, 0.0, 1.0),
    vec4(0.5, 0.5, 0.0, 1.0),
    vec4(-0.5, -0.5, 0.0, 1.0),
    vec4(0.5, -0.5, 0.0, 1.0)
);

const vec2 uvs[4] = vec2[](
    vec2(0, 0),
    vec2(1, 0),
    vec2(0, 1),
    vec2(1, 1)
);

layout(location = 0) in vec2 sprite;
layout(location = 1) in vec2 size;

layout(location = 0) out vec2 fsin_UV;

layout (set = 0, binding = 0) uniform OrthographicProjection
{
    mat4 projection;
    mat4 view;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
    mat4 model[1024];
};

void main()
{
    gl_Position = projection * view * model[gl_InstanceIndex] * positions[gl_VertexIndex];
    fsin_UV = (sprite + uvs[gl_VertexIndex]) * size;
}";

        private const string _fragmentCode = @"
#version 450
layout (set = 1, binding = 1) uniform texture2D SpriteTex; 
layout (set = 1, binding = 2) uniform sampler SpriteSampler;

layout(location = 0) in vec2 fsin_UV;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_UV);
}";

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public DrawSprites(EntityManager entityManager, GraphicsDevice graphicsDevice, Sdl2Window window)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _entityManager = entityManager;
            _graphicsDevice = graphicsDevice;
            _window = window;
            _landLayerResults = _entityManager.Find(
                builder => builder.Require<LocalToWorldMatrix>().Require<SpriteRenderer>().Require<LandLayer>()
            );
            _propLayerResults = _entityManager.Find(
                builder => builder.Require<LocalToWorldMatrix>().Require<SpriteRenderer>().Require<PropLayer>()
            );

            _cursourLayerResults = _entityManager.Find(
                builder => builder.Require<LocalToWorldMatrix>().Require<SpriteRenderer>().Require<CursourLayer>()
            );

            CreateResources();
        }

        private void CreateResources()
        {
            var factory = _graphicsDevice.ResourceFactory;

            ushort[] quadIndices = {0, 1, 2, 3};

            _vertexBuffer = factory.CreateBuffer(
                new BufferDescription(
                    2 * (uint) Unsafe.SizeOf<Vector2>() * 1024,
                    BufferUsage.VertexBuffer | BufferUsage.Dynamic
                )
            );
            _indexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

            var tex = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "Assets", "sprites.png"), false)
                .CreateDeviceTexture(_graphicsDevice, _graphicsDevice.ResourceFactory);
            var view = _graphicsDevice.ResourceFactory.CreateTextureView(tex);

            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

            _orthoBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint) Unsafe.SizeOf<Matrix4x4>() * 2,
                    BufferUsage.UniformBuffer | BufferUsage.Dynamic
                )
            );

            _orthoLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "OrthographicProjection",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Vertex
                    )
                )
            );
            _orthoSet = factory.CreateResourceSet(new ResourceSetDescription(_orthoLayout, _orthoBuffer));

            _modelBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint) Unsafe.SizeOf<Matrix4x4>() * 1024,
                    BufferUsage.UniformBuffer | BufferUsage.Dynamic
                )
            );

            _modelLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "WorldBuffer",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Vertex
                    ),
                    new ResourceLayoutElementDescription(
                        "SpriteTexture",
                        ResourceKind.TextureReadOnly,
                        ShaderStages.Fragment
                    ),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );

            _modelSet = factory.CreateResourceSet(
                new ResourceSetDescription(_modelLayout, _modelBuffer, view, _graphicsDevice.PointSampler)
            );

            var vertexLayout = new VertexLayoutDescription(
                (uint) Unsafe.SizeOf<Vector2>() * 2,
                1,
                new VertexElementDescription(
                    "sprite",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2
                ),
                new VertexElementDescription(
                    "size",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2
                )
            );

            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(_vertexCode),
                "main"
            );
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(_fragmentCode),
                "main"
            );

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            var pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState =
                    new RasterizerStateDescription(
                        FaceCullMode.None,
                        PolygonFillMode.Solid,
                        FrontFace.Clockwise,
                        true,
                        false
                    ),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new[] {_orthoLayout, _modelLayout},
                ShaderSet = new ShaderSetDescription(new[] {vertexLayout}, _shaders),
                Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _commandList = factory.CreateCommandList();
        }

        private static readonly Vector2 _tileSize = new Vector2(16f / 112, 16f / 832);

        public void Update()
        {
            var localToWorldMatrixes = _landLayerResults.GetComponentArray<LocalToWorldMatrix>();
            var sprites = _landLayerResults.GetComponentArray<SpriteRenderer>();

            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            _commandList.SetVertexBuffer(0, _vertexBuffer);
            //_commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _orthoSet);
            _commandList.SetGraphicsResourceSet(1, _modelSet);

            var ortho = _graphicsDevice.Map<Matrix4x4>(_orthoBuffer, MapMode.Write);
            ortho[0] = Matrix4x4.CreateOrthographicOffCenter(0, _window.Width, 0, _window.Height, 0, 1);
            ortho[1] = Matrix4x4.CreateTranslation(
                new Vector3(_window.Width / 2f - 25 * 16f, _window.Height / 2f - 25 * 16f, 0)
            );
            _graphicsDevice.Unmap(_orthoBuffer);

            var i = 0;
            DrawLayer(localToWorldMatrixes, sprites, _landLayerResults, ref i);
            DrawLayer(localToWorldMatrixes, sprites, _propLayerResults, ref i);
            /*if(i > 0) 
            {
                Draw((uint)i);
                i = 0;
            }*/

            //_commandList.UpdateBuffer(_orthoBuffer, 0, (Matrix4x4.CreateOrthographicOffCenter(0, _window.Width, 0, _window.Height, 0, 1), Matrix4x4.Identity));

            DrawLayer(localToWorldMatrixes, sprites, _cursourLayerResults, ref i);
            if (i > 0)
            {
                Draw((uint) i);
            }

            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
            _graphicsDevice.WaitForIdle();
        }

        private void DrawLayer(
            ComponentAccessArray<LocalToWorldMatrix> localToWorldMatrixes,
            ComponentAccessArray<SpriteRenderer> sprites,
            QueryResult results,
            ref int i)
        {
            foreach (ref readonly var idx in results)
            {
                _modelArray[i] = localToWorldMatrixes[idx].Value;
                _vertexArray[i] = (sprites[idx].Value, _tileSize);

                ++i;
                if (i > 1023)
                {
                    Draw((uint) i);
                    i = 0;
                }
            }
        }

        private void Draw(uint amount)
        {
            _commandList.UpdateBuffer(_vertexBuffer, 0, ref _vertexArray[0], sizeof(float) * 4 * 1024);
            _commandList.UpdateBuffer(_modelBuffer, 0, ref _modelArray[0], sizeof(float) * 16 * 1024);
            _commandList.Draw(4, amount, 0, 0);
        }

        public void Dispose()
        {
            _pipeline.Dispose();
            foreach (var shader in _shaders)
            {
                shader.Dispose();
            }

            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
}