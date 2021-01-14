using Godot;
using System.Collections.Generic;

public class VoxelGrid : Node2D
{
    public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;
    private ArrayMesh mesh;

    private List<Vector2> vertices;
    private List<int> triangles;

    [Export] Texture activeTexture;
    [Export] public int resolution;
    [Export] PackedScene voxelPrefab;

    private Voxel[] voxels;
    private MeshInstance2D[] voxelMeshes;

    private float voxelSize, gridSize;

    private Voxel dummyX, dummyY, dummyT;

    public void Initialize(int resolution, float size)
    {
        this.resolution = resolution;
        gridSize = size;
        voxelSize = size / resolution;
        voxels = new Voxel[resolution * resolution];
        voxelMeshes = new MeshInstance2D[voxels.Length];

        dummyX = new Voxel();
        dummyY = new Voxel();
        dummyT = new Voxel();

        for (int i = 0, y = 0; y < resolution; y++) {
            for (int x =0; x < resolution;x ++, i++) {
                CreateVoxel(i, x, y);
            }
        }

        GetNode<MeshInstance2D>("MeshInstance2D").Mesh = mesh = new ArrayMesh();
        vertices = new List<Vector2>();
        triangles = new List<int>();
        Refresh();
    }

    private void CreateVoxel(int i, int x, int y) {
        MeshInstance2D o = (MeshInstance2D) voxelPrefab.Instance();
        AddChild(o);
        o.Position = new Vector2((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize);
        o.ZIndex = 1;
        o.Scale = Vector2.One * voxelSize * 0.1f;
        voxelMeshes[i] = o;
        voxels[i] = new Voxel(x, y, voxelSize);
    }

    private void SetVoxelColors() {
        for (int i = 0; i < voxels.Length; i++) {
            voxelMeshes[i].Texture = voxels[i].state ? activeTexture : null;
        }
    }

    public void Apply(VoxelStencil stencil) {
		int xStart = stencil.XStart;
		if (xStart < 0) {
			xStart = 0;
		}
		int xEnd = stencil.XEnd;
		if (xEnd >= resolution) {
			xEnd = resolution - 1;
		}
		int yStart = stencil.YStart;
		if (yStart < 0) {
			yStart = 0;
		}
		int yEnd = stencil.YEnd;
		if (yEnd >= resolution) {
			yEnd = resolution - 1;
		}

		for (int y = yStart; y <= yEnd; y++) {
			int i = y * resolution + xStart;
			for (int x = xStart; x <= xEnd; x++, i++) {
				voxels[i].state = stencil.Apply(x, y, voxels[i].state);
			}
		}
		Refresh();
    }

    private void Refresh() {
        SetVoxelColors();
        Triangulate();
    }

    private void Triangulate() {
        vertices.Clear();
        triangles.Clear();
        mesh = new ArrayMesh();

        if (xNeighbor != null) {
            dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
        }
        TriangulateCellRows();
        if (yNeighbor != null) {
            TriangulateGapRow();
        }

        if (triangles.Count > 1) {
            Godot.Collections.Array arrays = new Godot.Collections.Array();
            arrays.Resize( (int) ArrayMesh.ArrayType.Max);
            arrays[(int) ArrayMesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int) ArrayMesh.ArrayType.Index] = triangles.ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        GetNode<MeshInstance2D>("MeshInstance2D").Mesh = mesh;
    }

	private void TriangulateCellRows () {
		int cells = resolution - 1;
		for (int i = 0, y = 0; y < cells; y++, i++) {
			for (int x = 0; x < cells; x++, i++) {
				TriangulateCell(
					voxels[i],
					voxels[i + 1],
					voxels[i + resolution],
					voxels[i + resolution + 1]);
			}
            if (xNeighbor != null) {
                TriangulateGapCell(i);
            }
		}
	}

	private void TriangulateCell (Voxel a, Voxel b, Voxel c, Voxel d) {
        int cellType = 0;
		if (a.state) {
			cellType |= 1;
		}
		if (b.state) {
			cellType |= 2;
		}
		if (c.state) {
			cellType |= 4;
		}
		if (d.state) {
			cellType |= 8;
		}
        switch (cellType)
        {
            case 0:
                return;
		    case 1:
			    AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
			    break;
            case 2:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                break;
            case 4:
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 8:
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;

    		case 3:
                AddQuad(a.position, a.yEdgePosition, b.yEdgePosition, b.position);
                break;
            case 5:
                AddQuad(a.position, c.position, c.xEdgePosition, a.xEdgePosition);
                break;
            case 10:
                AddQuad(a.xEdgePosition, c.xEdgePosition, d.position, b.position);
                break;
            case 12:
                AddQuad(a.yEdgePosition, c.position, d.position, b.yEdgePosition);
                break;
            case 15:
                AddQuad(a.position, c.position, d.position, b.position);
                break;
    		case 7:
                AddPentagon(a.position, c.position, c.xEdgePosition, b.yEdgePosition, b.position);
                break;
            case 11:
                AddPentagon(b.position, a.position, a.yEdgePosition, c.xEdgePosition, d.position);
                break;
            case 13:
                AddPentagon(c.position, d.position, b.yEdgePosition, a.xEdgePosition, a.position);
                break;
            case 14:
                AddPentagon(d.position, b.position, a.xEdgePosition, a.yEdgePosition, c.position);
                break;
    		case 6:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 9:
                AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;
		}
	}

	private void AddTriangle (Vector2 a, Vector2 b, Vector2 c) {
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	private void AddQuad (Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	private void AddPentagon (Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 e) {
		int vertexIndex = vertices.Count;
		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);
		vertices.Add(e);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 3);
		triangles.Add(vertexIndex + 4);
	}

    private void TriangulateGapCell (int i) {
		Voxel dummySwap = dummyT;
		dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
		dummyT = dummyX;
		dummyX = dummySwap;
		TriangulateCell(voxels[i], dummyT, voxels[i + resolution], dummyX);
	}

    private void TriangulateGapRow () {
		dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
		int cells = resolution - 1;
		int offset = cells * resolution;

		for (int x = 0; x < cells; x++) {
			Voxel dummySwap = dummyT;
			dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
			dummyT = dummyY;
			dummyY = dummySwap;
			TriangulateCell(voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
		}

		if (xNeighbor != null) {
			dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
			TriangulateCell(voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
		}
	}
}
