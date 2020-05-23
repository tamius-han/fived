using Godot;
using System;
using System.Diagnostics;

public class StartButton : Button {

  public override void _Pressed() {
    GetTree().ChangeScene("res://scenes/World.tscn");
  }

}
