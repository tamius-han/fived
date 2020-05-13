using System;
using Godot;

namespace PlanetTopology {
  public class PlanetCell : IEquatable<PlanetCell>{

    // vertices
    public PlanetVertex a;
    public PlanetVertex b;
    public PlanetVertex c;

    // their respective indices
    public int ai;
    public int bi;
    public int ci;

    public bool isEdgeAB;
    public bool isEdgeAC;
    public bool isEdgeBC;

    public PlanetCell(PlanetVertex a, PlanetVertex b, PlanetVertex c) {
      this.a = a;
      this.b = b;
      this.c = c;

      // edges are false by default
      this.isEdgeAB = false;
      this.isEdgeAC = false;
      this.isEdgeBC = false;
    }

    public PlanetCell(PlanetVertex a, PlanetVertex b, PlanetVertex c, int ai, int bi, int ci) {
      this.a = a;
      this.b = b;
      this.c = c;

      this.ai = ai;
      this.bi = bi;
      this.ci = ci;

      // Note: ADDING POINTS TO EACH OTHER'S NEIGHBOR LIST IS HARAM AS FUCK AT THIS POINT
      // Since we're calling this construction on _every_ subdivision, adding neighbors here
      // rather than in post will only result in us adding way too many points to the 
      // neighbor list. And that is rather bad.

      // edges are false by default
      this.isEdgeAB = false;
      this.isEdgeAC = false;
      this.isEdgeBC = false;
    }

    public void SetNeighbors() {
      this.a.AddNeighbor(this.b);
      this.a.AddNeighbor(this.c);
      this.b.AddNeighbor(this.a);
      this.b.AddNeighbor(this.c);
      this.c.AddNeighbor(this.a);
      this.c.AddNeighbor(this.b);
    }

    // public void SetPolygons() {
    //   this.a.AddPolygon(this)
    //   this.b.AddPolygon(this)
    //   this.c.AddPolygon(this)
    // }

    public void AddToSurfaceTool(SurfaceTool surfaceTool) {
      // System.Diagnostics.Debug.WriteLine("This vertex A-B-C: [" + a.x + ", " + a.y + ", " + a.z + "] - [" + b.x + ", " + b.y + ", " + b.z + "] - [" + c.x + ", " + c.y + ", " + c.z + "]");

      const float minLimit = -0.05f;
      const float maxLimit = 0.069f;

      surfaceTool.AddColor(a.determineVertexColor(minLimit, maxLimit));
      surfaceTool.AddColor(b.determineVertexColor(minLimit, maxLimit));
      surfaceTool.AddColor(c.determineVertexColor(minLimit, maxLimit));

      // Add UV coords
      surfaceTool.AddUv(
        new Vector2(
          Mathf.Atan2(a.z, a.x) / (2f * Mathf.Pi),
          Mathf.Asin(a.y) / Mathf.Pi + 0.5f
        )
      );
      surfaceTool.AddUv(
        new Vector2(
          Mathf.Atan2(b.z, b.x) / (2f * Mathf.Pi),
          Mathf.Asin(b.y) / Mathf.Pi + 0.5f
        )
      );
      surfaceTool.AddUv(
        new Vector2(
          Mathf.Atan2(c.z, c.x) / (2f * Mathf.Pi),
          Mathf.Asin(c.y) / Mathf.Pi + 0.5f
        )
      );

      // add vertices
      surfaceTool.AddVertex(new Vector3(a.x, a.y, a.z));
      surfaceTool.AddVertex(new Vector3(b.x, b.y, b.z));
      surfaceTool.AddVertex(new Vector3(c.x, c.y, c.z));
    }

    // override object.Equals
    public override bool Equals(object obj) {
      if (obj == null || GetType() != obj.GetType()) {
          return false;
      }
      
      return this.a.Equals(((PlanetCell)obj).a) && this.b.Equals(((PlanetCell)obj).b) && this.c.Equals(((PlanetCell)obj).c);
    }
    public bool Equals(PlanetCell c) {
      return this.a.Equals(c.a) && this.b.Equals(c.b) && this.c.Equals(c.c);
    }
    
    // override object.GetHashCode
    public override int GetHashCode() {
      return this.a.GetHashCode();
    }
  }
}
