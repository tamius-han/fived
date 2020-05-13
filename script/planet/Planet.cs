using Godot;
using PlanetGeometry;
using PlanetTopology;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Godot.Mesh;
using static Godot.SpatialMaterial;

public class Planet : MeshInstance {
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  [Export]
  public int seed = 1337;
  [Export]
  public SpatialMaterial material;
  [Export]
  private ArrayMesh planetMesh;




  // private static int subdivide(List<Vector3> vertices, int point1Index, int point2Index)
  // {

  // }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
	  GenerateSphere(9);
	  // GenerateSphere(9);
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
   public override void _Process(float delta) {
     Debug.Write(".");
     RotateX(delta/ 24);
     RotateY(delta / 8);
   }

  // Generates sphere with radius 1
  public void GenerateSphere(int iterations) {
    float radius = 10f;

    System.Diagnostics.Debug.WriteLine("Generating sphere!");
    Stopwatch totalTimer = new Stopwatch();
    totalTimer.Start();
    
    

    SurfaceTool surfaceTool = new SurfaceTool();
    this.planetMesh = new ArrayMesh();

    material.VertexColorUseAsAlbedo = true;
    surfaceTool.SetMaterial(material);
    surfaceTool.Begin(PrimitiveType.Triangles);

    // GENERATE SPHERE:
    Stopwatch planetGeneratorTimer = new Stopwatch();
    planetGeneratorTimer.Start();

    PlanetGenerator pg = new PlanetGenerator();
    // 9 gives ~30s generate time, which is acceptable
    PlanetData[] planetData = pg.GenerateBaseTopology(iterations, radius);
    // PlanetData[] planetData = pg.GenerateBaseTopology(8);

    planetGeneratorTimer.Stop();
    TimeSpan ts = planetGeneratorTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Sphere generated. Number of faces: " + (planetData[0].faces.Count * 20) + "; time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    // todo: create biomes

    // Add perlino
    Stopwatch perlinTimer = new Stopwatch();
    perlinTimer.Start();

    foreach (PlanetData data in planetData) {
      data.vertices = pg.AddPerlinDisplacement(data.vertices, radius, 16, 0.42f);

      float maxDispalcement = 0;
      float maxNegDisplacement = 0;
      foreach (PlanetVertex pv in data.vertices) {
        if (pv.h > maxDispalcement) {
          maxDispalcement = pv.h;
        }
        if (pv.h < maxNegDisplacement) {
          maxNegDisplacement = pv.h;
        }
      }
      Debug.WriteLine("Max displacement for this face (from-to) " + maxNegDisplacement +" -> " + maxDispalcement);
    }

    perlinTimer.Stop();
    ts = planetGeneratorTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Perlin applied. Time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    Stopwatch surfaceToolTimer = new Stopwatch();
    surfaceToolTimer.Start();

    foreach (PlanetData data in planetData) {
      foreach (PlanetCell c in data.faces) {
        c.AddToSurfaceTool(surfaceTool);
      }
      foreach (PlanetCell c in data.middleFaces) {
        c.AddToSurfaceTool(surfaceTool);
      }
    }
    surfaceTool.GenerateNormals();
    surfaceTool.Commit(this.planetMesh);

    surfaceToolTimer.Stop();
    ts = surfaceToolTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("SurfaceTool generated our mesh. Time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);
    totalTimer.Stop();
    ts = totalTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("————————————————— total time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds + "—————————————————");

    this.Mesh = planetMesh;
  }
}
