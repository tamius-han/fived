using Godot;
using System;
using System.Collections.Generic;

namespace PlanetTopology {
  public class PlanetTopologyData {

    // List of edge cells contain _all_ cells between two vertices (except the second vertex)
    // e.g. abEdgeVertices contains all vertices between vertex A and B, including A and excluding B.
    // On the level of a single cell, each edgeVertices list contains only a single point
    public List<PlanetVertex> vertices;
    public List<PlanetCell> faces;
    public List<PlanetCell> middleFaces;
    public EdgeData edgeCells;

    public PlanetTopologyData(List<PlanetVertex> vertices, List<PlanetCell> faces, List<PlanetCell> middleFaces, EdgeData edgeCells) {
      this.vertices = vertices;
      this.faces = faces;
      this.middleFaces = middleFaces;
      this.edgeCells = edgeCells;
    }
  }

  public class EdgeData {
    public List<PlanetCell> abCells;
    public List<PlanetCell> bcCells;
    public List<PlanetCell> acCells;

    public EdgeData(int subDivisions) {
      int cells = 1 << subDivisions;

      this.abCells = new List<PlanetCell>(cells);
      this.bcCells = new List<PlanetCell>(cells);
      this.acCells = new List<PlanetCell>(cells);
    }
  }
}
