// GhostGames 2025

using System;
using System.Collections.Generic;
using FlaxEngine;

/// <summary>
/// RenderTextureSetup Script.
/// </summary>
public class RenderTextureSetup : Script
{
    Camera Cam;

    [Serialize, ShowInEditor]
    MaterialBase Material;


    [Limit(256, 2000), Serialize, ShowInEditor]
    Float2 Resolution
    {
        get => _resolution;
        set
        {
            value = Float2.Clamp(value, new Float2(1), new Float2(2000));
            if (_resolution != value)
            {
                _resolution = value;
                if (_output)
                {
                    // Resize backbuffer
                    UpdateOutput();
                }
            }
        }
    }

    [Serialize, ShowInEditor]
    float ViewDistance = 50000;

    [Serialize, ShowInEditor]
    StaticModel TargetMesh;

    private Float2 _resolution = new Float2(512, 512);
    GPUTexture _output;
    private SceneRenderTask _task;
    private MaterialInstance _material;

    [Serialize, ShowInEditor]
    bool ActorLocked;
    [Serialize, ShowInEditor]
    Actor TargetLockedActor;

    [Serialize, ShowInEditor]
    float VerticalOffset;


    Vector3 Offset = new Vector3();
    public override void OnStart()
    {
        //Setup in OnStart so you have to do less
        Cam = (Actor as Camera);
        Cam.OrthographicSize = 5000;//This is only temporary until bug #4053 is merged
        TargetMesh.SetParent(Actor, true, false);
        Offset.Y = VerticalOffset;
    }









    private void UpdateOutput()
    {
        var desc = GPUTextureDescription.New2D(
            (int)_resolution.X,
            (int)_resolution.Y,
            PixelFormat.R8G8B8A8_UNorm);
        _output.Init(ref desc);
    }

    public override void OnEnable()
    {
        // Create backbuffer
        if (_output == null)
            _output = new GPUTexture();
        UpdateOutput();

        // Create rendering task
        if (_task == null)
            _task = new SceneRenderTask();
        _task.Order = -100;
        _task.Camera = Cam;
        _task.Output = _output;
        _task.ViewFlags =
            ViewFlags.Reflections |
            ViewFlags.Decals |
            ViewFlags.AO |
            ViewFlags.GI |
            ViewFlags.DirectionalLights |
            ViewFlags.PointLights |
            ViewFlags.SpotLights |
            ViewFlags.SkyLights |
            ViewFlags.Sky |
            ViewFlags.Shadows |
            ViewFlags.SpecularLight |
            ViewFlags.CustomPostProcess |
            ViewFlags.ToneMapping;
        _task.Enabled = false;

        if (Material && _material == null)
        {
            // Use dynamic material instance
            if (Material.WaitForLoaded())
                throw new Exception("Failed to load material.");
            _material = Material.CreateVirtualInstance();

            // Set render task output to draw on model
            _material.SetParameterValue("Image", _output);
            TargetMesh.SetMaterial(0, _material);
            // Bind material to parent model
            if (Actor is StaticModel staticModel && staticModel.Model)
            {
                staticModel.Model.WaitForLoaded();
                staticModel.SetMaterial(0, _material);
            }
        }

        _task.Enabled = true;
    }

    public override void OnUpdate()
    {
        if (ActorLocked)
        {
            Actor.Position = TargetLockedActor.Position + Offset;
        }



        // Optimize by disabling rendering if main game view is far too far
        var mainView = MainRenderTask.Instance.View;
        _task.Enabled = Vector3.Distance(Actor.Position, mainView.Origin + mainView.Position) <= ViewDistance;
    }

    public override void OnDisable()
    {
        // Unbind temporary material
        if (Actor is StaticModel staticModel && staticModel.Model && staticModel.Model.IsLoaded)
            staticModel.SetMaterial(0, Material);

        // Ensure to cleanup resources
        Destroy(ref _task);
        Destroy(ref _output);
        Destroy(ref _material);
    }
}