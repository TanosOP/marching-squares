using Godot;
using System;

public class VoxelMap : Area2D
{
    [Export] public float size = 2f;

    [Export] public int voxelResolution = 8;
    [Export] public int chunkResolution = 2;

    [Export] PackedScene voxelGridPrefab;

    private VoxelGrid[] chunks;
    private bool mouseInRange = false;
    private int fillTypeIndex, radiusIndex, stencilIndex;

    private VoxelStencil[] stencils = {
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private float chunkSize, voxelSize, halfSize;

    public override void _Ready()
    {
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
        if (Input.IsActionPressed("click")){
            if (mouseInRange) {
                EditVoxels(ToLocal(GetGlobalMousePosition()));
            }
        }
    }

    private void CreateChunk(int i, int x, int y) {
        VoxelGrid chunk = (VoxelGrid) voxelGridPrefab.Instance();
        chunk.Initialize(voxelResolution, chunkSize);
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

    private void EditVoxels(Vector2 point) {
		int centerX = (int)((point.x + halfSize) / voxelSize);
		int centerY = (int)((point.y + halfSize) / voxelSize);

		int xStart = (centerX - radiusIndex - 1) / voxelResolution;
		if (xStart < 0) {
			xStart = 0;
		}
		int xEnd = (centerX + radiusIndex) / voxelResolution;
		if (xEnd >= chunkResolution) {
			xEnd = chunkResolution - 1;
		}
		int yStart = (centerY - radiusIndex - 1) / voxelResolution;
		if (yStart < 0) {
			yStart = 0;
		}
		int yEnd = (centerY + radiusIndex) / voxelResolution;
		if (yEnd >= chunkResolution) {
			yEnd = chunkResolution - 1;
		}

		VoxelStencil activeStencil = stencils[stencilIndex];
		activeStencil.Initialize(fillTypeIndex == 0, radiusIndex);

		int voxelYOffset = yEnd * voxelResolution;
		for (int y = yEnd; y >= yStart; y--) {
			int i = y * chunkResolution + xEnd;
			int voxelXOffset = xEnd * voxelResolution;
			for (int x = xEnd; x >= xStart; x--, i--) {
				activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
				chunks[i].Apply(activeStencil);
				voxelXOffset -= voxelResolution;
			}
			voxelYOffset -= voxelResolution;
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

    void OnRadiusSpinBoxValueChanged(float value) {
        radiusIndex = (int) value;
    }
}
