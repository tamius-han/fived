using Godot;
using System;

public enum PenteractFace : byte {
  // types in this enum are ordered in a way that scales nicely with
  // number of dimensions
  North = 0,        // |  |  |  |  |
  South = 1,        // |  |  |  |  |++——— faces for 1D
  East = 2,         // |  |  |  |
  West = 3,         // |  |  |  |++——— faces for 2D
  Front = 4,        // |  |  |
  Back = 5,         // |  |  |++——— faces for 3D
  Inner = 6,        // |  |   
  Outer = 7,        // |  |++——— faces for 4D
  DoubleInner = 8,  // |   
  DoubleOuter = 9   // |++——— faces for 5D
}

public class PenteractData {
  public TesserractData[] tesserracts;

  public PenteractData() {
    this.tesserracts = new TesserractData[10];
    for (int i = 0; i < 10; i++) {
      this.tesserracts[i] = new TesserractData((PenteractFace)i);
    }

    // constructor for tesserract data already created planet graph,
    // we only need to connect the twin planets
    this.CreateTesserractGraph();
  }

  // Determines how tesseracts are connected (read: it connects
  // planets with their twin planet). It works exactly like 
  // connecting planets in TesseractData, except with one extra
  // dimension
  public void CreateTesserractGraph() {
    // Each tesserract is connected to 8 different planets.
    // Just like with GeneratePlanetGraph() in TesserractData,
    // connection to self leads to DoubleOuter and connection
    // to the opposite leads to DoubleInner.

    for (int i = 0; i < 8; i++) {  // for every tesserract that we have
      for (int j = i + 1; j < 8; j++) {  // for every planet in a tesserract
        // replace connections to opposite tesseracts with connections to
        // the double inner tesserract
        if (i == j - 1 && i % 2 == 0) {
          this.tesserracts[i].SetConnectionTo((PenteractFace)j, (PenteractFace)i, this.tesserracts[(int)PenteractFace.DoubleInner]);
          continue;
        }

        this.tesserracts[i].SetConnectionTo((PenteractFace)j, (PenteractFace)i, this.tesserracts[j]);
        // Q&A time: why is 'j' planetPositionFrom & 'i' planetPositionTo?
        //
        // Let's say that i=North and j=East. 
        // Eastern planet of north tesserract should connect to the northern planet of eastern tesserract.
        // 
        // This function call thus translates to:
        // northTesserract.easternPlanet <---> easternTesserract.northernPlanet
      }

      // connect 'self' direction to double outer. Note that positions in
      // double outer are inverted compared to what we're used to.
      PenteractFace oppositePosition;
      if (i % 2 == 0) {
        oppositePosition = (PenteractFace)(i + 1);
      } else {
        oppositePosition = (PenteractFace)(i - 1);
      }
      this.tesserracts[i].SetConnectionTo((PenteractFace)i, oppositePosition, this.tesserracts[(int)PenteractFace.DoubleOuter]);
      // Q&A: why is 'i' planetPositionFrom here?
      // 
      // To continue with previous example where i=North. Here, we want to connect northern planet to somewhere.
      // Since we can't connect to self, we need to connect to the DoubleOuter tesserract. 
      // Since DoubleOuter tesserract is inverted, we aren't connecting north:north, but north:south.
      // 
      // This function call thus translates to:
      // northTesserract.northPlanet <----> doubleOuterTesserract.southPlanet
    }
  }
}
