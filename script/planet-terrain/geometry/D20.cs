using System.Collections.Generic;
using PlanetTopology;

namespace PlanetGeometry {
  public class D20 {
    static float tao = 1.618033988749895f;

    public PlanetVertex[] vertices;
    public List<PlanetCell> faces;

    private PlanetVertex[] getVertices() {

      PlanetVertex[] vertices = {
        new PlanetVertex(-1, tao, 0),
        new PlanetVertex(1, tao, 0),
        new PlanetVertex(-1, -tao, 0),
        new PlanetVertex(1, -tao, 0),

        new PlanetVertex(0, -1, tao),
        new PlanetVertex(0, 1, tao),
        new PlanetVertex(0, -1, -tao),
        new PlanetVertex(0, 1, -tao),

        new PlanetVertex(tao, 0, -1),
        new PlanetVertex(tao, 0, 1),
        new PlanetVertex(-tao, 0, -1),
        new PlanetVertex(-tao, 0, 1),
      };

      return vertices;
    }

    private List<PlanetCell> getFaces(PlanetVertex[] v) {
      // d20 only knows 20 sides
      List<PlanetCell> faces = new List<PlanetCell>(20);

      faces.Add(new PlanetCell(v[0], v[5], v[11]));
      faces.Add(new PlanetCell(v[0], v[1], v[5]));
      faces.Add(new PlanetCell(v[0], v[7], v[1]));
      faces.Add(new PlanetCell(v[0], v[10], v[7]));
      faces.Add(new PlanetCell(v[0], v[11], v[10]));

      faces.Add(new PlanetCell(v[1], v[9], v[5]));
      faces.Add(new PlanetCell(v[5], v[4], v[11]));
      faces.Add(new PlanetCell(v[11], v[2], v[10]));
      faces.Add(new PlanetCell(v[10], v[6], v[7]));
      faces.Add(new PlanetCell(v[7], v[8], v[1]));

      faces.Add(new PlanetCell(v[3], v[4], v[9]));
      faces.Add(new PlanetCell(v[3], v[2], v[4]));
      faces.Add(new PlanetCell(v[3], v[6], v[2]));
      faces.Add(new PlanetCell(v[3], v[8], v[6]));
      faces.Add(new PlanetCell(v[3], v[9], v[8]));

      faces.Add(new PlanetCell(v[4], v[5], v[9]));
      faces.Add(new PlanetCell(v[2], v[11], v[4]));
      faces.Add(new PlanetCell(v[6], v[10], v[2]));
      faces.Add(new PlanetCell(v[8], v[7], v[6]));
      faces.Add(new PlanetCell(v[9], v[1], v[8]));

      return faces;
    }

    public D20() {
      this.vertices = this.getVertices();
      this.faces = this.getFaces(this.vertices);
    }
    public D20(float radius) {
      this.vertices = this.getVertices();

      foreach (PlanetVertex v in this.vertices) {
        v.FixVertexPoint(radius);
      }

      this.faces = this.getFaces(this.vertices);
    }
  }
}