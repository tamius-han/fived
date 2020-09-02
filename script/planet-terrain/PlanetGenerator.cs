using Biome;
using Godot;
using PlanetGeometry;
using PlanetTopology;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

  [Flags]
  public enum D20FaceEdge {
	NotAnEdge = 0,
	AB = 1,
	BC = 2,
	CA = 4,
  MiddleFace = 8,
  }

public class PlanetGenerator {
  
  public string currentStatus = "";

  public PlanetTopologyData[] GenerateBaseTopologyRecursive(int subdivisionIterations, float radius){
    D20 baseGeometry = new D20(radius);
    
    // variables for progress display
    // int totalVertices = 3 * (int)Math.Pow(2, subdivisionIterations + 1) * 20;
    // int[] currentVertices[] = new int[20];


    List<PlanetCell> faces = new List<PlanetCell>(baseGeometry.faces);

    // Since we'll do lots and lots o' topology, it would be beneficial if we parallelize
    // certain things. We'll do this optimization later.
    int cpuCores = System.Environment.ProcessorCount;

    List<PlanetVertex>[] vertices = new List<PlanetVertex>[20];
    for (int i = 0; i < vertices.Length; i++) {
      vertices[i] = new List<PlanetVertex>(3 * (int)Math.Pow(2, subdivisionIterations + 1));
    }

    List<PlanetCell>[] newFaces = new List<PlanetCell>[20];
    List<PlanetCell>[] middleFaces = new List<PlanetCell>[20];
    for (int i = 0; i < newFaces.Length; i++) {
      newFaces[i] = new List<PlanetCell>(4 * (int)Math.Pow(2, subdivisionIterations + 1));
      // middleFaces[i] = new List<PlanetCell>((int)Math.Pow(2, subdivisionIterations + 1));
      middleFaces[i] = new List<PlanetCell>(0);
    }

    EdgeData[] edgeCells = new EdgeData[20];
    for (int i = 0; i < newFaces.Length; i++) {
      edgeCells[i] = new EdgeData(subdivisionIterations);
    }

    PlanetTopologyData[] data = new PlanetTopologyData[20];
    
    Stopwatch planetGeneratorTimer = new Stopwatch();
    planetGeneratorTimer.Start();

    // subdivide faces
    // Parallel.For(0, faces.Count, new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
    Parallel.For(0, faces.Count, new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
      SubdivideFaceRecursively(faces[i], vertices[i], newFaces[i], middleFaces[i], radius, (D20FaceEdge.AB | D20FaceEdge.BC | D20FaceEdge.CA | D20FaceEdge.MiddleFace), edgeCells[i], vertices[i].Capacity, subdivisionIterations);

      // neighbors need to be processed _after_ we've finished subdividing faces. We can't do that during subdivisions.
      for (int ci = 0; ci < newFaces[i].Count; ci++) {
        newFaces[i][ci].SetNeighbors();
      }
      data[i] = new PlanetTopologyData(vertices[i], newFaces[i], middleFaces[i], edgeCells[i]);
    });

    planetGeneratorTimer.Stop();
    TimeSpan ts = planetGeneratorTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Time needed for subdivisions: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    // stitch faces together
    Stopwatch stitchTimer = new Stopwatch();
    stitchTimer.Start();
    object stitchLock = new object();
    Parallel.For(0, faces.Count - 1,  new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
      StitchEdgeBruteForce(i, edgeCells, stitchLock);
    });

    stitchTimer.Stop();
    ts = stitchTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Time needed for stitching: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);


    return data;
  }

  public PlanetTopologyData[] GenerateBaseTopology(int subdivisions, float radius){
    D20 baseGeometry = new D20(radius);
    
    // variables for progress display
    // int totalVertices = 3 * (int)Math.Pow(2, subdivisionIterations + 1) * 20;
    // int[] currentVertices[] = new int[20];

    List<PlanetCell> startingFaces = new List<PlanetCell>(baseGeometry.faces);

    // Since we'll do lots and lots o' topology, it would be beneficial if we parallelize
    // certain things. We'll do this optimization later.
    int cpuCores = System.Environment.ProcessorCount;

    List<PlanetVertex>[] vertices = new List<PlanetVertex>[20];
    List<PlanetCell>[] faces = new List<PlanetCell>[20];

    PlanetTopologyData[] data = new PlanetTopologyData[20];
    
    Stopwatch planetGeneratorTimer = new Stopwatch();
    planetGeneratorTimer.Start();

    // subdivide faces
    Parallel.For(0, startingFaces.Count, new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
      object[] result = SubdivideFace(startingFaces[i], subdivisions, radius);

      vertices[i] = (List<PlanetVertex>)result[0];
      faces[i] = (List<PlanetCell>)result[1];

      // neighbors need to be processed _after_ we've finished subdividing faces. We can't do that during subdivisions.
      for (int ci = 0; ci < faces[i].Count; ci++) {
        faces[i][ci].SetNeighbors();
      }
      data[i] = new PlanetTopologyData(vertices[i], faces[i]);
    });

    planetGeneratorTimer.Stop();
    TimeSpan ts = planetGeneratorTimer.Elapsed;
    System.Diagnostics.Debug.WriteLine("Time needed for subdivisions: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);

    // stitch faces together
    // TODO

    // Stopwatch stitchTimer = new Stopwatch();
    // stitchTimer.Start();
    // object stitchLock = new object();
    // Parallel.For(0, faces.Count - 1,  new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
    //   StitchEdgeBruteForce(i, edgeCells, stitchLock);
    // });

    // stitchTimer.Stop();
    // ts = stitchTimer.Elapsed;
    // System.Diagnostics.Debug.WriteLine("Time needed for stitching: " + ts.Minutes + "m " + ts.Seconds + "." + ts.Milliseconds);


    return data;
  }

  public List<PlanetVertex> AddPerlinDisplacement(List<PlanetVertex> vertices, float radius, int iterations, float persistence) {
    int cpuCores = System.Environment.ProcessorCount;
    OpenSimplexNoise simplex = new OpenSimplexNoise();

    // NOTE — these must be provided in correct order!
    LandscapeTypeConf landscapeConf = new LandscapeTypeConf();
    landscapeConf.AddTypeConf(LandscapeType.Plains, 0.0f, 0.0f, 0.2f);
    landscapeConf.AddTypeConf(LandscapeType.Hills, 0.3f, 0.05f, 0.6f);
    landscapeConf.AddTypeConf(LandscapeType.Hills, 0.4f, 0.1f, 0.8f);
    landscapeConf.AddTypeConf(LandscapeType.Mountains, 0.6f, 0.069f, 1.25f);
    landscapeConf.AddTypeConf(LandscapeType.Mountains, 0.7f, 0.69f, 2.5f);
    landscapeConf.AddTypeConf(LandscapeType.Mountains, 0.9f, 0.69f, 3.3f);
    landscapeConf.AddTypeConf(LandscapeType.Mountains, 1.7f, 0.69f, 5.0f);

    int seed = 69420;
    int octaves = 8;
    float period = 0.42f * radius;
    persistence = 0.37f * (radius / 8);

    simplex.Seed = seed;
    simplex.Octaves = octaves;                   // number of repetitions
    simplex.Period = period;                     // the smaller the number, the more the details
    simplex.Persistence = persistence;           // the bigger the number, the more prominent the smaller details are

    OpenSimplexNoise landscapeTypeSimplex = new OpenSimplexNoise();
    landscapeTypeSimplex.Seed = seed + 69;
    landscapeTypeSimplex.Octaves = octaves;
    landscapeTypeSimplex.Period = period;
    landscapeTypeSimplex.Persistence = persistence;

    // each thread gets a chunk of 1000 vertices to process at a time
    int step = 1000;

    Parallel.For(0, (int)Math.Ceiling((float)vertices.Count / (float)step), new ParallelOptions {MaxDegreeOfParallelism = cpuCores}, i => {
      int maxI = (i + 1) * step;
      if (maxI > vertices.Count) {
        maxI = vertices.Count;
      }
      float displacement, displacementMultiplier;

      for (int j = i * step; j < maxI; j++) {
        displacement = simplex.GetNoise3d(vertices[j].x, vertices[j].y, vertices[j].z);

        // this guarantees mountain ridges. 
        // Note that the correct function is 1 - abs(x), but we're using smaller numbers in order to guarantee seas
        displacement = 0.2f - (displacement < 0 ? -displacement: displacement);
        
        displacementMultiplier = landscapeConf.GetHeightMultiplierForValue((landscapeTypeSimplex.GetNoise3d(vertices[j].x, vertices[j].y, vertices[j].z) + 1f) * 0.5f);

        Vector3 tmp = new Vector3(vertices[j].x, vertices[j].y, vertices[j].z);
        Vector3 vecDisplacement = (new Vector3(vertices[j].x, vertices[j].y, vertices[j].z) / tmp.Length()) * displacement * displacementMultiplier;
        tmp += vecDisplacement;
        vertices[j].Update(tmp.x, tmp.y, tmp.z, displacement);
      }
    });

    return vertices;
  }

  public void DisplacePoles() {
    // Keep in mind that we are displacing _all_ six poles: north, south, east, west, front/top, back/bottom

    // TODO: implement
  }

  public void GenerateLandscapeTypes(List<PlanetVertex>[] vertices, List<LandscapeTypeConf> biomeConfs, int biomeSeeds, int rngSeed, float minRandomStrength, float maxRandomStrength) {
    // int cpuCores = System.Environment.ProcessorCount;

    // int totalVertices = vertices[0].Count * vertices.Length;
    // int randomInt, initialVertexIndex, initialFaceIndex;

    // PlanetVertex vertex;

    // Random random = new Random(rngSeed);

    // // we won't parallelize this right away
    // foreach (LandscapeTypeConf biomeConf in biomeConfs) {
    //   for (int i = 0; i < ((int)Math.Ceiling(biomeConf.rarity * (float)biomeSeeds)); i++) {
    //     randomInt = random.Next(totalVertices);
    //     initialFaceIndex = randomInt / vertices.Length;
    //     initialVertexIndex = randomInt % vertices.Length;

    //     vertex = vertices[initialFaceIndex][initialVertexIndex];

    //     // every vertex should have at least 5 neighbors. If it has less than that,
    //     // we're looking at a vertex that shouldn't even exist — so we reroll
    //     if (vertex.neighbors.Count < 5) {
    //       i--;
    //       continue;
    //     }
    //   }
    // }

  }

  #region face-subdivision

  //   /**
  //    *   This is the series of triangles we want to get
  //    *
  //    *         Arrangement           Triangle abbreviations
  //    *                                  
  //    *              /\                       *
  //    *             /CC\                     / \
  //    *            /    \                   / C \
  //    *           /      \                 /_____\
  //    *          /AC    BC\              / \     / \
  //    *         /__________\            / A \ M / B \
  //    *        /\BM      AM/\          /_____\ /_____\   
  //    *       /CA\        /CB\
  //    *      /    \      /    \
  //    *     /      \    /      \
  //    *    /AA    BA\CM/AB    BB\
  //    *   /__________\/__________\
  //    *
  //    *   We arrange triangles in this way to make joining them a bit more sense.
  //    *   
  //    *   When joining edges, points on the edges around middle triangle are deduplicated
  //    *   and points around the outer edges are concatenated. When deduplicating, we must
  //    *   remember that points in edges of the inner (M) triangle are referenced in the 
  //    *   opposite order than they are on the edges of the 
  //    *
  //   */

  
  private void SubdivideFaceRecursively(PlanetCell cell, List<PlanetVertex> newVertices, List<PlanetCell> newFaces, List<PlanetCell> middleFaces, float radius, D20FaceEdge mainEdges, EdgeData outerEdges, int cacheSize, int iterations) {
    Dictionary<long, int> vertexCache = new Dictionary<long, int>(cacheSize);

    newVertices.Add(cell.a);
    newVertices.Add(cell.b);
    newVertices.Add(cell.c);

    cell.ai = 0;
    cell.bi = 1;
    cell.ci = 2;

    this.SubdivideFaceRecursively(cell, ref newVertices, newFaces, middleFaces, vertexCache, mainEdges, outerEdges, radius, iterations);
  }

  private void SubdivideFaceRecursively(PlanetCell cell, ref List<PlanetVertex> newVertices, List<PlanetCell> newFaces, List<PlanetCell> middleFaces, Dictionary<long, int> vertexCache, D20FaceEdge mainEdges, EdgeData outerEdges, float radius, int iterations) {
    // Debug.WriteLine("edge: " + mainEdges + " --> " + (D20FaceEdge.AB | D20FaceEdge.BC));

    if (iterations == 0) {
      newFaces.Add(cell);

      if (mainEdges == D20FaceEdge.NotAnEdge) {
        return;
      }
      // if we're on the outer edge, we also add the face (not the vertex) to the outer edge structure
      if ((mainEdges & D20FaceEdge.AB) != 0) {
        outerEdges.abCells.Add(cell);
      } 
      if ((mainEdges & D20FaceEdge.BC) != 0) {
        outerEdges.bcCells.Add(cell);
      }
      if ((mainEdges & D20FaceEdge.CA) != 0) {
        outerEdges.acCells.Add(cell);
      }

      return;
    }

    int abIndex = this.SubdivideEdge(cell.ai, cell.bi, ref vertexCache, ref newVertices, radius);
    int bcIndex = this.SubdivideEdge(cell.bi, cell.ci, ref vertexCache, ref newVertices, radius);
    int caIndex = this.SubdivideEdge(cell.ci, cell.ai, ref vertexCache, ref newVertices, radius);

    // Create four new faces and handle them recursively
    PlanetCell va = new PlanetCell(
    // vertices
    cell.a,
    newVertices[abIndex],
    newVertices[caIndex],
    // indices
    cell.ai,
    abIndex,
    caIndex
    );
    PlanetCell vb = new PlanetCell(
    // indices
    newVertices[abIndex],
    cell.b,
    newVertices[bcIndex],
    
    // vertices
    abIndex,
    cell.bi,
    bcIndex
    );
    PlanetCell vc = new PlanetCell(
    // indices
    newVertices[caIndex],
    newVertices[bcIndex],
    cell.c,

    // vertices
    caIndex,
    bcIndex,
    cell.ci
    );
    PlanetCell vm = new PlanetCell(
    // indices
    newVertices[bcIndex],
    newVertices[caIndex],
    newVertices[abIndex],

    // vertices
    bcIndex,
    caIndex,
    abIndex
    );

    SubdivideFaceRecursively(va, ref newVertices, newFaces, middleFaces, vertexCache, mainEdges & (D20FaceEdge.AB | D20FaceEdge.CA), outerEdges, radius, iterations - 1);
    SubdivideFaceRecursively(vb, ref newVertices, newFaces, middleFaces, vertexCache, mainEdges & (D20FaceEdge.AB | D20FaceEdge.BC), outerEdges, radius, iterations - 1);
    SubdivideFaceRecursively(vc, ref newVertices, newFaces, middleFaces, vertexCache, mainEdges & (D20FaceEdge.CA | D20FaceEdge.BC), outerEdges, radius, iterations - 1);

    // Middle faces _always_ get a special mark. Since the previous three calls strip away MiddleFace bit, the final 
    // level of recursion will only have "middle" leaf nodes marked. We store those nodes separately, since these
    // "middle faces" duplicate points for topology. We still add them to the list, though, mostly because render
    // still needs them (even if the topology doesn't)
    SubdivideFaceRecursively(vm, ref newVertices, newFaces, middleFaces, vertexCache, D20FaceEdge.MiddleFace, outerEdges, radius, iterations - 1);
  }

  private void AddVertexIndexToCache(int index, ref Dictionary<long, int> vertexCache) {
	  vertexCache.Add(index, index);
  }

  private int SubdivideEdge(int indexA, int indexB, ref Dictionary<long, int> cache, ref List<PlanetVertex> vertices, float radius) {
    long key;
    int index;

    if (indexA < indexB) {
      key = ((long)indexA << 32) + indexB;
    } else {
      key = ((long)indexB << 32) + indexA;
    }

    if (cache.TryGetValue(key, out index)) {
      // System.Diagnostics.Debug.WriteLine("have index " + index + "in cache. Vertex get: [" + vertices[index].x + ", " + vertices[index].y + ", " + vertices[index].z + "]");
      return index;
    }

    try {
      PlanetVertex a = vertices[indexA];
      PlanetVertex b = vertices[indexB];

      // System.Diagnostics.Debug.WriteLine("Subdividing a: [" + a.x + ", " + a.y + ", " + a.z + "] and b [" + b.x + ", " + b.y + ", " + b.z + "]");


      PlanetVertex newVertex = new PlanetVertex(
      (a.x + b.x) / 2f,
      (a.y + b.y) / 2f,
      (a.z + b.z) / 2f
      );
      newVertex.FixVertexPoint(radius);

      // System.Diagnostics.Debug.WriteLine("new vertex: [" + newVertex.x + ", " + newVertex.y + ", " + newVertex.z + "] will have index " + vertices.Count);

      index = vertices.Count;
      vertices.Add(newVertex);
      cache.Add(key, index);

      return index;
    } catch (Exception e) {
      System.Diagnostics.Debug.WriteLine("indexA: " + indexA + "; index B: " + indexB + "; vertices size:" + vertices.Count + "; vertices cap: " + vertices.Capacity);
    throw e;
    }
  }
  #endregion

  #region face-subdivision-iterative
    //   /**
    //    *   THIS PROBABLY WARRANTS REPEATING
    //    *
    //    *   This is the series of triangles we want to get
    //    *
    //    *         Arrangement           Triangle abbreviations
    //    *                                  
    //    *              /\                       *
    //    *             /CC\                     / \
    //    *            /    \                   / C \
    //    *           /      \                 /_____\
    //    *          /AC    BC\              / \     / \
    //    *         /__________\            / A \ M / B \
    //    *        /\BM      AM/\          /_____\ /_____\   
    //    *       /CA\        /CB\
    //    *      /    \      /    \
    //    *     /      \    /      \
    //    *    /AA    BA\CM/AB    BB\
    //    *   /__________\/__________\
    //    *
    //    *   We arrange triangles in this way to make joining them a bit more sense.
    //    *   
    //    *   When joining edges, points on the edges around middle triangle are deduplicated
    //    *   and points around the outer edges are concatenated. When deduplicating, we must
    //    *   remember that points in edges of the inner (M) triangle are referenced in the 
    //    *   opposite order than they are on the edges of the 
    //    *
    //   */

    private object[] SubdivideFace(PlanetCell cell, int subdivisions, float radius) {
      /* Let's see how many vertices and faces we can expect from this:
       *
       *   where n = subdivisions + 1:
       * 
       *   # of vertices:     (n² + n) / 2
       *   # of faces:        n² / 2
       *
       * This is according to napkin math.
       */ 

      int segments = subdivisions + 1;
      int newFaceCount = (segments * segments) / 2;
      int newVertexCount = 3 + (subdivisions * 3) + (newFaceCount / 2);

      List<PlanetVertex> newVertices = new List<PlanetVertex>(newVertexCount);
      List<PlanetCell> newFaces = new List<PlanetCell>(newFaceCount);


      float[] deltasAB = {
        (cell.b.x - cell.a.x) / (float)segments,
        (cell.b.y - cell.a.y) / (float)segments,
        (cell.b.z - cell.a.z) / (float)segments
      };

      float[] deltasAC = {
        ((cell.c.x - cell.a.x) / (float)segments) - deltasAB[0],
        ((cell.c.y - cell.a.y) / (float)segments) - deltasAB[1],
        ((cell.c.z - cell.a.z) / (float)segments) - deltasAB[2]
      };

      // this is an exception and we can do it outside the loop, so ... we're gonna do it outside the loop.

      PlanetVertex newA = new PlanetVertex(cell.a.x, cell.a.y, cell.a.z);
      PlanetVertex newB = new PlanetVertex(cell.b.x, cell.b.y, cell.b.z);
      PlanetVertex newC = new PlanetVertex(cell.c.x, cell.c.y, cell.c.z);

      /**
       *  Imagine we have a triangle. Here's how we pick new spots
       *  Coords: i,j                                               (NOTE: NOT ACTUAL VERTEX COORDS)
       *
       *  j
       *  
       *  A            .
       *  |           .
       *  |          .
       *  |         2,2
       *  |
       *  |      1,1   2,1
       *  |
       *  |    0,0  1,0  2,0 . . .
       *
       *  x    ——————————————————————> i
       *
       *  As we can see, j coordinate will never get bigger than i because that's how triangles work.
       *
       *  Now we only need to convert these "on triangle" coordinates into "real" vertex coordinates 
       *  (x,y,z). Since the triangle is a flat object, we can just interpolate between the vertices
       *  using linear interpolation. 
       *  
       *  Interpolating is _very_ easy on the edges, but it gets a bit trickier inside the triangle
       *  since we'd have to use interpolation to get coordinates.
       *
       *
       *                       But do we really need interpolation, tho?
       *
       *  Turns out, we don't. Triangles are flat and our points will be peppered around in regular
       *  intervals. This means that we can calculate the distance between the vertices of the triangle
       *  we want to subdivide (and when we say 'distance', we mean delta x,y,z), and divide the 
       *  distance/those deltas by number of subdivisions.
       * 
       *  Coordinates of the new triangle are determined the following formula:
       *
       *        x = Δx[a-b] * i  +  Δx[a-c] * j      | where Δx[a-b] is (B[x] - A[x])
       *        y = Δy[a-b] * i  +  Δy[a-c] * j      | and   Δx[a-c] is (C[x] - A[x])
       *        z = Δz[a-b] * i  +  Δz[a-c] * j      | Same applies for y and z. 
       *
       *
       *  Let's briefly cover how faces and vertices are created:
       *
       *  j              .
       *                .
       *  A            .                     Quick definitions
       *  |           9 . . .                
       *  |                                    * corner-facing triangle:
       *  |         5   8 . . .                  a triangle that faces the lower left corner 
       *  |                                      a.k.a. vertex 0
       *  |       2   4   7  . . .             
       *  |                                    * edge-facing triangle:
       *  |     0   1   3   6  10 . . .          a triangle that faces away from vertex 0
       *
       *  x    ——————————————————————> i
       *
       *  Here we can see that we need at least two (2) vertices on the edge-to-edge line
       *  in order to form a triangle. For every iteration of the inner loop (starting at 
       *  the second vertex), we can make two triangles:
       *       
       *       * corner-facing: (i-1, j-1), (i-1, j), (i,j)        example: vertices 1-3-4
       *       * edge-facing:   (i-1, j-1), (i,j), (i, j-1)        example: vertices 1-4-2
       *  
       *  Most importantly, if i==j, we don't get to draw the edge-facing triangle (think
       *  you can tell why from the picture).
       *  
       *  We also do few other things to make accessing the j-1 vertices a bit easier — 
       *  we keep the vertices.length() - 1 (that is, index of last element) from the start
       *  of the previous pass in a variable somewhere.
       *  
       *  Oh and by the way — instead of creating the "leftover" i==j triangle at the end 
       *  of the loop, we can group creating the pair of edge-facing and corner-facing
       *  triangles a bit differently, create the "leftover" triangle at the start in the
       *  outer loop and lose us an unnecessary if statement.
       */

      // we define those outside of the for loop. We'll pick 'while' to do some early 
      // optimizations ... hopefully.
      int i = 0;
      int j = 0;
      int last_j = 0;

      // this is our i=0 iteration, right here
      newVertices.Add(newA);
      newA.FixVertexPoint(radius);


      // do note that the pylon operator means first loop iteration will be i=1 and
      // last iteration will have i=subdivisions ... which is what we want in this case.
      while (i ++< subdivisions) {
        j = 0;

        // at this point, last_j is about i from where we want it to be. Remember — blank
        // vertices.length - 1 gives us the (i-1, i-1) vertex, but we want (i-1, 0)
        last_j = newVertices.Count - i;  

        // add new vertices for the first triangle
        PlanetVertex b = new PlanetVertex(
          newA.x + (deltasAB[0] * (float)i + deltasAC[0] * (float)(i - j)),
          newA.y + (deltasAB[1] * (float)i + deltasAC[1] * (float)(i - j)),
          newA.z + (deltasAB[2] * (float)i + deltasAC[2] * (float)(i - j))
        );

        j = 1;
        PlanetVertex c = new PlanetVertex(
          newA.x + (deltasAB[0] * (float)i + deltasAC[0] * (float)(i - j)),
          newA.y + (deltasAB[1] * (float)i + deltasAC[1] * (float)(i - j)),
          newA.z + (deltasAB[2] * (float)i + deltasAC[2] * (float)(i - j))
        );
        newVertices.Add(b);
        newVertices.Add(c);
        b.FixVertexPoint(radius);
        c.FixVertexPoint(radius);
        
        // build first triangle
        newFaces.Add(
          new PlanetCell(
            newVertices[last_j],
            c,
            b
          )
        );

        // build the rest of the stuff
        while (j ++< i) {
          b = c;                      // save the reference to the previous "current" vertex
          c = new PlanetVertex(       // create new vertex
            newA.x + (deltasAB[0] * (float)i + deltasAC[0] * (float)(i - j)),
            newA.y + (deltasAB[1] * (float)i + deltasAC[1] * (float)(i - j)),
            newA.z + (deltasAB[2] * (float)i + deltasAC[2] * (float)(i - j))
          );
          newVertices.Add(c);
          c.FixVertexPoint(radius);

          // build the edge-facing triangle
          newFaces.Add(
            new PlanetCell(
              newVertices[last_j],
              newVertices[++last_j],   // increment before using — saves us an op
              b
            )
          );
          // build the corner-facing triangle
          newFaces.Add(
            new PlanetCell(
              newVertices[last_j],
              c,
              b
            )
          );
        }
      }

      // System.Diagnostics.Debug.WriteLine("————————————————————————————————————————— iteration finished ————————————————————————————————————————————————");

      return new object[] {(object)newVertices, (object)newFaces};
    }
  #endregion

  #region edge-stitch helpers
  public void StitchEdgeBruteForce(int face, EdgeData[] edgeCells, object stitchLock) {
    // only look for common edges with faces that have higher index.
    // faces with indices lower than <face> will already be merged
    // by this time.

    int lastIndex = 0;

    // find edge that matches ab
    for (int i = face + 1; i < edgeCells.Length; i++) {
      lastIndex = edgeCells[i].abCells.Count - 1;

      // Since triangles share orientation (all CW or all CCW, we can assume the points will flow in the opposite directions)
      // Exceptions are points on the CA edge, which are ordered from A to C due to technical reasons

      // AB EDGE OF FACE
      // this is legal, because the edges of two triangles run in the opposite directions & edge point is in both triangles. What is more — due to 
      // concurrency reasons, we will ignore the last point on the list, too
      if (edgeCells[face].abCells[0].a.Equals(edgeCells[i].abCells[lastIndex].b) && edgeCells[face].abCells[lastIndex].a.Equals(edgeCells[i].abCells[0].b)) {
        StitchEdge(edgeCells[face].abCells, edgeCells[i].abCells, true, D20FaceEdge.AB, D20FaceEdge.AB, stitchLock);
        continue; // each edge is between two triangles at most, so we don't need to check further
      }
      // AB edge on this face matches BC face on the other
      if (edgeCells[face].abCells[0].a.Equals(edgeCells[i].bcCells[lastIndex].c) && edgeCells[face].abCells[lastIndex].a.Equals(edgeCells[i].bcCells[0].c)) {
        StitchEdge(edgeCells[face].abCells, edgeCells[i].bcCells, true, D20FaceEdge.AB, D20FaceEdge.BC, stitchLock);
        continue;
      }
      // AB edge on one face matches CA edge on the other
      if (edgeCells[face].abCells[0].a.Equals(edgeCells[i].acCells[0].a) && edgeCells[face].abCells[lastIndex].a.Equals(edgeCells[i].acCells[lastIndex].a)) {
        StitchEdge(edgeCells[face].abCells, edgeCells[i].acCells, false, D20FaceEdge.AB, D20FaceEdge.CA, stitchLock);
        continue;
      }

      // BC EDGE OF FACE
      // BC on this face maches AC on the other
      if (edgeCells[face].bcCells[0].b.Equals(edgeCells[i].abCells[lastIndex].b) && edgeCells[face].bcCells[lastIndex].b.Equals(edgeCells[i].abCells[0].b)) {
        StitchEdge(edgeCells[face].bcCells, edgeCells[i].abCells, true, D20FaceEdge.BC, D20FaceEdge.AB, stitchLock);
        continue;
      }
      // shared BC edge
      if (edgeCells[face].bcCells[0].b.Equals(edgeCells[i].bcCells[lastIndex].c) && edgeCells[face].bcCells[lastIndex].b.Equals(edgeCells[i].bcCells[0].c)) {
        StitchEdge(edgeCells[face].bcCells, edgeCells[i].bcCells, true, D20FaceEdge.BC, D20FaceEdge.BC, stitchLock);
        continue;
      }
      // BC edge on this is AC edge on the other
      if (edgeCells[face].bcCells[0].b.Equals(edgeCells[i].acCells[0].a) && edgeCells[face].bcCells[lastIndex].b.Equals(edgeCells[i].acCells[lastIndex].a)) {
        StitchEdge(edgeCells[face].bcCells, edgeCells[i].acCells, false, D20FaceEdge.BC, D20FaceEdge.CA, stitchLock);
        continue;
      }
      
      // AC EDGE OF FACE
      if (edgeCells[face].acCells[0].a.Equals(edgeCells[i].abCells[0].a) && edgeCells[face].acCells[lastIndex].a.Equals(edgeCells[i].abCells[lastIndex].a)) {
        StitchEdge(edgeCells[i].abCells, edgeCells[face].acCells, false, D20FaceEdge.BC, D20FaceEdge.AB, stitchLock);
        continue;
      }
      // shared BC edge
      if (edgeCells[face].acCells[0].a.Equals(edgeCells[i].bcCells[0].b) && edgeCells[face].acCells[lastIndex].a.Equals(edgeCells[i].bcCells[lastIndex].b)) {
        StitchEdge(edgeCells[i].bcCells, edgeCells[face].acCells, false, D20FaceEdge.BC, D20FaceEdge.BC, stitchLock);
        continue;
      }
      // // BC edge on this is AC edge on the other
      if (edgeCells[face].acCells[0].a.Equals(edgeCells[i].acCells[lastIndex].a) && edgeCells[face].acCells[lastIndex].a.Equals(edgeCells[i].acCells[0].a)) {
        StitchEdge(edgeCells[i].acCells, edgeCells[face].acCells, true, D20FaceEdge.BC, D20FaceEdge.CA, stitchLock);
        continue;
      }
    }
  }

  private void StitchEdge(List<PlanetCell> edgeCells, List<PlanetCell> otherEdgeCells, Boolean reverse, D20FaceEdge edge, D20FaceEdge otherEdge, object stitchLock) {
    int i, j, j_increment, i_loopLimit;
    if (reverse) {
      i = 0;
      j = otherEdgeCells.Count - 1;
      j_increment = -1;
    } else {
      i = 0;
      j = 0;
      j_increment = 1;
    }
    i_loopLimit = edgeCells.Count - 2;

    // corner vertex syc may happen in multiple threads at once
    if (edgeCells.Count > 1) {
      lock (stitchLock) {
        // first iteration needs to happen outside the loop and inside the lock!
        if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
            otherEdgeCells[j].b = edgeCells[i].a;
            otherEdgeCells[j].a = edgeCells[i].b;
        } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
            otherEdgeCells[j].b = edgeCells[i].b;
            otherEdgeCells[j].c = edgeCells[i].a;
        } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
            otherEdgeCells[i].a = edgeCells[i].a;
            otherEdgeCells[i].c = edgeCells[i].b;
        }
        if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
            otherEdgeCells[i].b = edgeCells[i].b;
            otherEdgeCells[i].a = edgeCells[i].c;
        } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
            otherEdgeCells[j].c = edgeCells[i].b;
            otherEdgeCells[j].b = edgeCells[i].c;
        } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
            otherEdgeCells[j].a = edgeCells[j].b;
            otherEdgeCells[j].c = edgeCells[j].c;
        }
        if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
            otherEdgeCells[j].a = edgeCells[i].a;
            otherEdgeCells[j].b = edgeCells[i].c;
        } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
            otherEdgeCells[j].c = edgeCells[i].c;
            otherEdgeCells[j].b = edgeCells[i].a;
        } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
            otherEdgeCells[j].c = edgeCells[i].a;
            otherEdgeCells[j].a = edgeCells[i].c;
        }
        i++;
        j += j_increment;
      }

      // we trust all edges have the same number of points because that's how it's supposed to be
      if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
        /**
          *
          *      / \    / \
          *     /  b\  /a  \                              Cell order
          *  A /_____\/_____\ B    <——— edgeCells            --->
          *  B \    a/\b    / A    <——— otherEdgeCells       <---
          *     \   /  \   /
          *
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].b = edgeCells[i].a;  // edges run in the opposite direction here!
          otherEdgeCells[j].a = edgeCells[i].b;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
        /**
          *
          *      / \    / \
          *     /  b\  /a  \                             Cell order
          *  A /_____\/_____\ B    <——— edgeCells           ---> 
          *  C \    b/\c    / B    <——— otherEdgeCells      <---
          *     \   /  \   /
          *
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].b = edgeCells[i].b;
          otherEdgeCells[j].c = edgeCells[i].a;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
        /**
          *
          *      / \    / \
          *     /  b\  /a  \                             Cell order
          *  A /_____\/_____\ B    <——— edgeCells           --->
          *  A \    c/\a    / C    <——— otherEdgeCells      --->
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[i].a = edgeCells[i].a;
          otherEdgeCells[i].c = edgeCells[i].b;
          i++;
          j += j_increment;
        }
      }
      if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
        /**
          *
          *      / \    / \
          *     /  c\  /b  \                             Cell order
          *  B /_____\/_____\ C    <——— edgeCells           --->
          *  B \    a/\b    / A    <——— otherEdgeCells      <---
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[i].b = edgeCells[i].b;
          otherEdgeCells[i].a = edgeCells[i].c;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
        /**
          *
          *      / \    / \
          *     /  c\  /b  \                             Cell order
          *  B /_____\/_____\ C    <——— edgeCells           --->
          *  C \    b/\c    / B    <——— otherEdgeCells      <---
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].c = edgeCells[i].b;
          otherEdgeCells[j].b = edgeCells[i].c;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
        /**
          *
          *      / \    / \
          *     /  c\  /b  \                             Cell order
          *  B /_____\/_____\ C    <——— edgeCells           --->
          *  A \    c/\a    / C    <——— otherEdgeCells      --->
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].a = edgeCells[j].b;
          otherEdgeCells[j].c = edgeCells[j].c;
          i++;
          j += j_increment;
        }
      }
      if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
        /**
          *
          *      / \    / \
          *     /  a\  /c  \                             Cell order
          *  C /_____\/_____\ A    <——— edgeCells           <---
          *  B \    a/\b    / A    <——— otherEdgeCells      <---
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].a = edgeCells[i].a;
          otherEdgeCells[j].b = edgeCells[i].c;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
        /**
          *
          *      / \    / \
          *     /  a\  /c  \                             Cell order
          *  C /_____\/_____\ A    <——— edgeCells           <---
          *  C \    b/\c    / B    <——— otherEdgeCells      <---
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].c = edgeCells[i].c;
          otherEdgeCells[j].b = edgeCells[i].a;
          i++;
          j += j_increment;
        }
      } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
        /**
          *
          *      / \    / \
          *     /  a\  /c  \                             Cell order
          *  C /_____\/_____\ A    <——— edgeCells           <---
          *  A \    c/\a    / C    <——— otherEdgeCells      --->
          *     \   /  \   /
          *
          */
        while (i < i_loopLimit) {
          otherEdgeCells[j].c = edgeCells[i].a;
          otherEdgeCells[j].a = edgeCells[i].c;
          i++;
          j += j_increment;
        }
      }
    }

    // last point also has lock
    lock (stitchLock) {
      if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
          otherEdgeCells[j].b = edgeCells[i].a;
          otherEdgeCells[j].a = edgeCells[i].b;
      } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
          otherEdgeCells[j].b = edgeCells[i].b;
          otherEdgeCells[j].c = edgeCells[i].a;
      } else if ((edge & D20FaceEdge.AB) == D20FaceEdge.AB && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
          otherEdgeCells[i].a = edgeCells[i].a;
          otherEdgeCells[i].c = edgeCells[i].b;
      }
      if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
          otherEdgeCells[i].b = edgeCells[i].b;
          otherEdgeCells[i].a = edgeCells[i].c;
      } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
          otherEdgeCells[j].c = edgeCells[i].b;
          otherEdgeCells[j].b = edgeCells[i].c;
      } else if ((edge & D20FaceEdge.BC) == D20FaceEdge.BC && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
          otherEdgeCells[j].a = edgeCells[j].b;
          otherEdgeCells[j].c = edgeCells[j].c;
      }
      if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.AB) == D20FaceEdge.AB) {
          otherEdgeCells[j].a = edgeCells[i].a;
          otherEdgeCells[j].b = edgeCells[i].c;
      } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.BC) == D20FaceEdge.BC) {
          otherEdgeCells[j].c = edgeCells[i].c;
          otherEdgeCells[j].b = edgeCells[i].a;
      } else if ((edge & D20FaceEdge.CA) == D20FaceEdge.CA && (otherEdge & D20FaceEdge.CA) == D20FaceEdge.CA) {
          otherEdgeCells[j].c = edgeCells[i].a;
          otherEdgeCells[j].a = edgeCells[i].c;
      }
    }
  }

  private void FixEdgePoint(PlanetVertex keep, PlanetVertex discard, PlanetVertex discardNext) {
    // remove next point from neighbor list:

    this.FixEdgePoint(keep, discard);
  }
  private void FixEdgePoint(PlanetVertex keep, PlanetVertex discard) {
    // switch all neighbors from 'discard' point to 'keep' point

    for(int i = 0; i < discard.neighbors.Count; i++) {

      // remove 'discard' from old neighbors
      discard.neighbors[i].neighbors.Remove(discard);

      // add old neighbors to 'keep' (and keep to old neighbors)
      try {
      keep.AddNeighbor(discard.neighbors[i]);
      } catch (Exception e) {
        Debug.WriteLine("accessing inexistent element. discard.neihgbors.length? " + discard.neighbors.Count + "; i: " + i);
      }
    }
    // discard = null;
  }

  #endregion

  #region biome-generation
  
  // private void StartLandscapeTypePropagation(LandscapeType type, int rngSeed, float initialStrength, float maxDecayStrength, int maxRecursionDepth) {
  //   OpenSimplexNoise simplex = new OpenSimplexNoise();

  //   simplex.Seed = rngSeed;
  //   simplex.Octaves = 8;                   // number of repetitions
  //   simplex.Period = 3.42f;                // the smaller the number, the more the details
  //   simplex.Persistence = 4.20f;           // the bigger the number, the more prominent the smaller details are


  // }
  
  // private void PropagateLandscapeType(PlanetVertex v, LandscapeType type, float strength, float maxDecayStrength, int maxRecursionDepth, OpenSimplex simplex) {
  //   float newStrength = strength - ((simplex.GetNoise3d(v.x, v.y, v.z) + 1) * maxDecayStrength);

  //   if (newStrength <= 0 || maxRecursionDepth <= 0) {
  //     return;
  //   }

  //   // populate unclaimed
  //   if (v.landscapeType == null) {
  //     v.landscapeType = new CellLandscapeType(type, newStrength);
  //   } else {
  //     // only propagate 
  //     if (v.landscapeType.score < newStrength) {
  //       v.landscapeType = new CellLandscapeType(type, newStrength);
  //     }
  //   }

  //   foreach (PlanetVertex neighbor in v.neighbors) {
  //     PropagateLandscapeType(neighbor, type, newStrength, maxDecayStrength, maxRecursionDepth - 1, simplex);
  //   }
  // }
  #endregion

}
