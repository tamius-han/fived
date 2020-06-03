using Godot;
using System;

public class TesserractData {
  public string name;
  public PenteractFace position;
  public PlanetData[] planets;

  public TesserractData() {
    this.planets = new PlanetData[8];

    // initialize planets:
    for (int i = 0; i < 8; i++) {
      this.planets[i] = new PlanetData((PenteractFace)i);
    }

    // Set up graph of all possible connections
    this.CreatePlanetGraph();
  }

  public TesserractData(PenteractFace position) : this() {
    this.position = position;
  }

  public void SetConnectionTo(PenteractFace planetPositionFrom, PenteractFace planetPositionTo, TesserractData target) {
    this.planets[(int)planetPositionFrom].SetConnectionTo(PlanetConnection.Twin, target.planets[(int)planetPositionTo]);
  }


  // determines which planet is neighbor to which planet
  private void CreatePlanetGraph() {
    for (int i = 0; i < 6; i++) {
      // we're guaranteed by PlanetData that connecting planet A to planet B
      // will also establish the connection in the opposite direction. By doing
      // this, we avoid re-establishing connections that already exist + we 
      // also ensure that j is always at least 1 higher than j. This eliminates 
      // one neighbor option (planet can't be neighbor with self). Connection
      // to "self" means connection to the outer planet.
      //
      // Note — PenteractFace and PlanetConnection share values for original
      // six directions, which allows us to do this.

      this.planets[i].SetConnectionTo((PlanetConnection)i, this.planets[(int)PenteractFace.Outer]);
      
      for (int j = i + 1; j < 6; j++) {
        if (i == j - 1 && i % 2 == 0) {
          // planets on the opposite faces cannot be connected directly. This 
          // eliminates the other neighbor option. Kind of. Connection in that
          // direction should lead to the inner planet.

          this.planets[i].SetConnectionTo((PlanetConnection)j, this.planets[(int)PenteractFace.Inner]);
          continue;
        }
        // we should now be left with the rest of connections that need no
        // further conversions

        this.planets[i].SetConnectionTo((PlanetConnection)j, this.planets[j]);
      }
    }

    // that concludes our graph of possible planet connections. Note that this 
    // function doesn't set the twin planets. 
  }
}
