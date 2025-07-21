using UnityEngine;
using Verse;

namespace GrowingZoneIcons.Source;

public class Settings : ModSettings {
  public Vector2 drawOffset = new(0.5f, 1.0f);
  public Vector2 drawScale = new(3f, 3f);
  public int activeShaderIndex = 49;
  public bool drawOnlyWhenSelectedZone = true;

  public override void ExposeData() {
    Scribe_Values.Look(ref drawOffset, "drawOffset");
    Scribe_Values.Look(ref activeShaderIndex, "activeShaderIndex", 0);
    Scribe_Values.Look(ref drawOnlyWhenSelectedZone, "drawOnlyWhenSelectedZone",
      true);
    Scribe_Values.Look(ref drawScale, "drawScale", new Vector2(2.5f, 2.5f));
    base.ExposeData();
  }
}