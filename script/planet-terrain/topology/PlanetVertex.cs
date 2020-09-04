using System;
using System.Collections.Generic;
using System.Diagnostics;
using Biome;
using Godot;

namespace PlanetTopology {
  [Flags]
  public enum PlanetVertexFlags {
    None = 0,
    Water = 1,
    Road = 2,
    Settlement = 4,
  }

  public class PlanetVertex : IEquatable<PlanetVertex> {
    public float x;
    public float y;
    public float z;

    // displacement
    public float h;

    public CellLandscapeType landscapeType;

    public Color determineVertexColor(float maxDepth, float maxHeight) {
      float normalizedH;
      if (this.h < 0) {
        normalizedH = this.h / maxDepth;
        // return Color.FromHsv(0.1f, 1f, 1f, 1f);


        return Color.FromHsv(
          0.66667f,                                      // hue = blue
          (2f * normalizedH) + 0.25f,                    // saturation — ramps up quickly
          1.5f - Mathf.Min(0.5f, normalizedH),                    // value: starts bright but then tapers off
          1f                                             // alpha
        );
      } else {
        // return Color.FromHsv(0.69f, 1f, 1f, 1f);

        normalizedH = this.h / maxHeight;

        return Color.FromHsv(
          0.69f - Mathf.Max(0.350f, normalizedH * 0.5f),   // start somewhere near blue-ish greens and move towards brown, eventually
          Mathf.Clamp(0.69f - (0.88f * normalizedH), 0, 1),                    // start at 69% saturated and then decrease as we get higher
          0.7f + normalizedH * 0.33f,
          0f
        );
      }
    }

    public PlanetVertexFlags flags;

    public List<PlanetVertex> neighbors;
    // public List<PlanetCell> polygons;

    public PlanetVertex(float x, float y, float z) {
      this.x = x;
      this.y = y;
      this.z = z;

      this.h = 0.0f;
      this.flags = PlanetVertexFlags.None;

      // a vertex can have at most this many neighbours. Most will have 6.
      this.neighbors = new List<PlanetVertex>(6); 
      // this.polygons = new List<PlanetCell>(6);
    }

    public PlanetVertex(PlanetVertex old, Vector3 position, float h) {
      this.x = position.x;
      this.y = position.y;
      this.z = position.z;
      this.h = h;
      this.flags = old.flags;
      this.neighbors = old.neighbors;
    }

    public void Update(float x, float y, float z, float h) {
      this.h = h;
      this.x = x;
      this.y = y;
      this.z = z;

      // Debug.WriteLine("Updating vertex position: " + x + ", " + y + ", " + z + "; displacement/h: " + h);
    }

    public void AddNeighbor(PlanetVertex neighbor) {
      foreach (PlanetVertex pv in this.neighbors) {
        if (pv.Equals(neighbor)) {
          return; // we already have that vertex
        }
      }
      // add new neighbor
      this.neighbors.Add(neighbor);
      if (this.neighbors.Count > 8) {
        Debug.WriteLine("More than 8 neighbours for this vertex! (6 neighbors for land + 1 twin on edge + 1 twin for portals). Neighbors:" + this.neighbors.Count) ;
      }
    }
    // public void AddPolygon(PlanetCell pc) {
    //   if (! this.polygons.Contains(pc)) {
    //     this.polygons.Add(pc);
    //   }
    // }

    public void FixVertexPoint() {
      Vector3 normalized = new Vector3(this.x, this.y, this.z).Normalized();
      this.x = normalized.x;
      this.y = normalized.y;
      this.z = normalized.z;
    }
    public void FixVertexPoint(float radius) {
      Vector3 tmp = new Vector3(this.x, this.y, this.z);
      tmp *= radius / tmp.Length();
      this.x = tmp.x;
      this.y = tmp.y;
      this.z = tmp.z;
    }

    public bool Equals(PlanetVertex cmp) {
      return this.x == cmp.x && this.y == cmp.y && this.z == cmp.z;
    }

    public override int GetHashCode() {
      return (x * y / z).GetHashCode();
    }
  }
}
