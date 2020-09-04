using Godot;
using PlanetGeometry;
using PlanetTopology;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static Godot.Mesh;
using static Godot.SpatialMaterial;

public class PlanetTerrain : MeshInstance {
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  [Export]
  public int seed = 1337;
  [Export]
  public SpatialMaterial material;
  [Export]
  private ArrayMesh planetMesh;

  
  private String currentStatus;
  private Boolean isGenerated;
  private PlanetGenerator pg;

  // private Label progressLabel;

  // private static int subdivide(List<Vector3> vertices, int point1Index, int point2Index)
  // {

  // }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    StartGenerate(1024);
    // StartGenerate(512);  // equivalent to 9 using recursive
    // StartGenerate(4);

    // StartGenerate(9);
	  // GenerateSphere(7);
	  // GenerateSphere(9);
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
   public override void _Process(float delta) {
     RotateX(delta/ 24);
     RotateY(delta / 8);

     //if (!this.isGenerated) {
     //  this.progressLabel.Text = currentStatus;
     //}
   }

  public void StartGenerate(int subdivisions) {
    this.pg = new PlanetGenerator();
    this.isGenerated = false;
    // this.progressLabel = (Label)GetNode("DefaultScene/LoadingScreen/Root/MainLayout/Inner/LoadingLayout/CurrentStatus");

    // FindN

    System.Threading.Thread t = new System.Threading.Thread(() => GenerateSphere(subdivisions));
    t.Start();
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

    // 9 gives ~30s generate time, which is acceptable
    this.currentStatus = "Generating base topology (this step can take long!)";
    PlanetTopologyData[] planetData = pg.GenerateBaseTopology(iterations, radius);

    planetGeneratorTimer.Stop();
    TimeSpan ts = planetGeneratorTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Sphere generated. Number of faces: " + (planetData[0].faces.Count * 20) + "; time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    // todo: create biomes

    // Add perlino
    Stopwatch perlinTimer = new Stopwatch();
    perlinTimer.Start();

    this.currentStatus = "Adding perlin noise ...";

    Parallel.For(0, planetData.Length, new ParallelOptions {MaxDegreeOfParallelism = System.Environment.ProcessorCount}, i => {
      PlanetTopologyData data = planetData[i];
      data.vertices = pg.AddPerlinDisplacement(data.vertices, radius, 16, 0.42f);

      // float maxDispalcement = 0;
      // float maxNegDisplacement = 0;
      // foreach (PlanetVertex pv in data.vertices) {
      //   if (pv.h > maxDispalcement) {
      //     maxDispalcement = pv.h;
      //   }
      //   if (pv.h < maxNegDisplacement) {
      //     maxNegDisplacement = pv.h;
      //   }
      // }
      // Debug.WriteLine("Max displacement for this face (from-to) " + maxNegDisplacement +" -> " + maxDispalcement);
    });

    perlinTimer.Stop();
    ts = perlinTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Perlin applied. Time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    Stopwatch surfaceToolTimer = new Stopwatch();
    surfaceToolTimer.Start();

    this.currentStatus = "Building planet mesh ...";

    // // Possible future for multithreading ahead. Might happen some day.
    //
    // SurfaceTool[] surfaceTool = new SurfaceTool[planetData.Length];
    // this.planetMeshes = new ArrayMesh[planetData.Length];

    // material.VertexColorUseAsAlbedo = true;

    // Parallel.For(0, planetData.Length, new ParallelOptions {MaxDegreeOfParallelism = System.Environment.ProcessorCount}, i => {
    //   surfaceTool[i] = new SurfaceTool();
    //   this.planetMeshes[i] = new ArrayMesh();

    //   surfaceTool[i].SetMaterial(material);
    //   surfaceTool[i].Begin(PrimitiveType.Triangles);

    //   for (int j = 0; j < planetData[i].faces.Count; j++) {
    //     planetData[i].faces[j].AddToSurfaceTool(surfaceTool[i]);
    //   }

    //   surfaceTool[i].GenerateNormals();
    //   surfaceTool[i].Commit(this.planetMeshes[i]);
    // });

    int faceCount;
    for(int i = 0; i < planetData.Length; i++) {
      faceCount = planetData[i].faces.Count;
      for(int j = 0; j < faceCount; j++) {
        planetData[i].faces[j].AddToSurfaceTool(surfaceTool);
      }
    }

    // foreach (PlanetTopologyData data in planetData) {
    //   foreach (PlanetCell c in data.faces) {
    //     c.AddToSurfaceTool(surfaceTool);
    //   }
    // }

    surfaceTool.GenerateNormals();
    surfaceTool.Commit(this.planetMesh);

    surfaceToolTimer.Stop();
    ts = surfaceToolTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("SurfaceTool generated our mesh. Time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);
    totalTimer.Stop();
    ts = totalTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("————————————————— total time needed: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds + "—————————————————");

    this.Mesh = planetMesh;
    this.currentStatus = "";
  }
}
