using Godot;
using System;

public enum PlanetConnection {
  North = 0,
  South = 1,
  East = 2,
  West = 3,
  Front = 4,
  Back = 5,
  Twin = 6

}

public enum PlanetConnectionState {
  Active = 1,
  Disabled = 0,
  TemporarilyDisabled = -1
}

public class Planet {
  public string name;
  public float radius;
  public PenteractFace position;

  public Planet[] connections;
  public PlanetConnectionState[] connectionStates;
  

  
  public Tesserract tesserract;

  public Planet() {
    this.connections = new Planet[7];
    this.connectionStates = new PlanetConnectionState[7];
  }
  public Planet(PenteractFace position) : this() {
    this.position = position;
  }

  public void SetConnectionTo(PlanetConnection connection, Planet planet) {
    this.connections[(int)connection] = planet;
    planet.SetConnectionFrom(connection, this);
  }

  // add connection that originates at [connection point] on [planet]
  public void SetConnectionFrom(PlanetConnection connection, Planet planet) {
    if (connection == PlanetConnection.Twin) {
      this.connections[(int)PlanetConnection.Twin] = planet;
    } else {
      // we need to invert these
      if (((int)connection) % 2 == 0) {
        this.connections[(int)connection + 1] = planet;
      } else {
        this.connections[(int)connection - 1] = planet;
      }
    }
  }
}
