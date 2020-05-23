using Godot;
using System;

public class Quit : Button {
  public override void _Pressed()  {
    GetTree().Quit(); // default behavior
  }
}
