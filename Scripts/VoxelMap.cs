using Godot;
using System;

public class VoxelMap : Area2D
{
    [Export] public float size = 2f;

    [Export] public int voxelResolution = 8;
    [Export] public int chunkResolution = 2;
    [Export] public bool snapToGrid;
    [Export] public float maxFeatureAngle = 135f;

    [Export] PackedScene voxelGridPrefab;
    [Export] NodePath stencilSquarePath;
    [Export] NodePath stencilCylinderPath;

    private VoxelGrid[] chunks;
    private bool mouseInRange = false;
    private MeshInstance2D[] stencilVisualizations;
    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private float chunkSize, voxelSize, halfSize;

    public override void _Ready()
    {
        stencilVisualizations = new MeshInstance2D[] {GetNode<MeshInstance2D>(stencilSquarePath), GetNode<MeshInstance2D>(stencilCylinderPath)};

        CollisionShape2D collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        RectangleShape2D rectangleShape2D = new RectangleShape2D();
        rectangleShape2D.Extents = new Vector2(size / 2, size / 2);
        collisionShape2D.Shape = rectangleShape2D;


        halfSize = size * 0.5f;
        chunkSize = size / chunkResolution;
        voxelSize = chunkSize / voxelResolution;

        chunks = new VoxelGrid[chunkResolution * chunkResolution];
        for (int i = 0, y = 0; y < chunkResolution; y++) {
            for (int x = 0; x < chunkResolution; x++, i++) {
                CreateChunk(i, x, y);
            }
        }
    }

    public override void _Process(float delta)
    {
        MeshInstance2D visualization = stencilVisualizations[stencilIndex];
        
        if (mouseInRange){
            Vector2 center = ToLocal(GetGlobalMousePosition());
			center.x += halfSize;
			center.y += halfSize;
            if (snapToGrid){
                center.x = ((int)(center.x / voxelSize) + 0.5f) * voxelSize;
                center.y = ((int)(center.y / voxelSize) + 0.5f) * voxelSize;
            }
            if (Input.IsActionPressed("click")) {
                EditVoxels(center);
            }

			center.x -= halfSize;
			center.y -= halfSize;
			visualization.Position = center;
			visualization.Scale = Vector2.One * ((radiusIndex + 0.5f) * voxelSize * 2f);
            visualization.Visible = true;
        } else {
            visualization.Visible = false;
        }
    }

    private void CreateChunk(int i, int x, int y) {
        VoxelGrid chunk = (VoxelGrid) voxelGridPrefab.Instance();
        chunk.Initialize(voxelResolution, chunkSize, maxFeatureAngle);
        AddChild(chunk);
        chunk.Position = new Vector2(x * chunkSize - halfSize, y * chunkSize - halfSize);
        chunks[i] = chunk;
		if (x > 0) {
			chunks[i - 1].xNeighbor = chunk;
		}
		if (y > 0) {
			chunks[i - chunkResolution].yNeighbor = chunk;
			if (x > 0) {
				chunks[i - chunkResolution - 1].xyNeighbor = chunk;
			}
		}
    }

	private void EditVoxels (Vector2 center) {
		VoxelStencil activeStencil = stencils[stencilIndex];
		activeStencil.Initialize(fillTypeIndex == 0, (radiusIndex + 0.5f) * voxelSize);
		activeStencil.SetCenter(center.x, center.y);

		int xStart = (int)((activeStencil.XStart - voxelSize) / chunkSize);
		if (xStart < 0) {
			xStart = 0;
		}
		int xEnd = (int)((activeStencil.XEnd + voxelSize) / chunkSize);
		if (xEnd >= chunkResolution) {
			xEnd = chunkResolution - 1;
		}
		int yStart = (int)((activeStencil.YStart - voxelSize) / chunkSize);
		if (yStart < 0) {
			yStart = 0;
		}
		int yEnd = (int)((activeStencil.YEnd + voxelSize) / chunkSize);
		if (yEnd >= chunkResolution) {
			yEnd = chunkResolution - 1;
		}

		for (int y = yEnd; y >= yStart; y--) {
			int i = y * chunkResolution + xEnd;
			for (int x = xEnd; x >= xStart; x--, i--) {
				activeStencil.SetCenter(center.x - x * chunkSize, center.y - y * chunkSize);
				chunks[i].Apply(activeStencil);
			}
		}
	}

    public void SetMouseInRangeTrue() { mouseInRange = true; }
    public void SetMouseInRangeFalse() { mouseInRange = false; }

    void OnFillOptionButtonItemSelected(int index) {
        fillTypeIndex = index;
    }

    void OnStencilOptionButtonItemSelected(int index) {
        stencilIndex = index;
    }

    void OnRadiusHSliderValueChanged(float value) {
        radiusIndex = (int) value;
    }
}
