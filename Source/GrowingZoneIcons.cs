using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GrowingZoneIcons.Source {
  public class GrowingZoneIcons : Mod {
    public static Settings Settings;
    public static readonly List<Shader> shaderOptions = [];

    public static Shader ActiveShader {
      get {
        if (Settings.activeShaderIndex < 0
          || Settings.activeShaderIndex >= shaderOptions.Count)
          return ShaderDatabase.Cutout;
        return shaderOptions[Settings.activeShaderIndex];
      }
    }

    public GrowingZoneIcons(ModContentPack content) : base(content) {
      Settings = GetSettings<Settings>();

      var harmony = new Harmony("xyz.msws.growingzoneicons");
      harmony.PatchAll();

      foreach (var field in typeof(ShaderDatabase).GetFields()) {
        if (field.FieldType != typeof(Shader)) continue;
        var shader = (Shader)field.GetValue(null);
        if (shader != null && !shaderOptions.Contains(shader)) {
          shaderOptions.Add(shader);
        }
      }
    }

    private Vector2 scrollPos = Vector2.zero;

    public override void DoSettingsWindowContents(Rect inRect) {
      // --- Top (Sticky) Controls ---
      var listingTop = new Listing_Standard();
      var topRect    = new Rect(inRect.x, inRect.y, inRect.width, 120f);
      listingTop.Begin(topRect);

      var textOffsetX = Settings.drawOffset.x.ToString("F2");
      var textOffsetY = Settings.drawOffset.y.ToString("F2");
      var textScaleX  = Settings.drawScale.x.ToString("F2");
      var textScaleY  = Settings.drawScale.y.ToString("F2");

      listingTop.TextFieldNumericLabeled("Draw Offset X",
        ref Settings.drawOffset.x, ref textOffsetX, -100, 100);
      listingTop.TextFieldNumericLabeled("Draw Offset Y",
        ref Settings.drawOffset.y, ref textOffsetY, -100, 100);

      listingTop.TextFieldNumericLabeled("Draw Scale X",
        ref Settings.drawScale.x, ref textScaleX, 0.1f, 10f);
      listingTop.TextFieldNumericLabeled("Draw Scale Y",
        ref Settings.drawScale.y, ref textScaleY, 0.1f, 10f);

      listingTop.CheckboxLabeled("Only Draw When Zone Is Selected",
        ref Settings.drawOnlyWhenSelectedZone,
        "If enabled, icons will only be drawn when a zone is selected.");

      listingTop.End();

      // --- Scrollable Shader List ---
      var scrollYStart = topRect.yMax + 10f;
      var itemHeight   = Text.LineHeight + 6f;
      var itemCount    = shaderOptions.Count(s => s != null);
      var viewHeight   = itemCount * itemHeight;

      var scrollRect = new Rect(inRect.x, scrollYStart, inRect.width,
        inRect.height - scrollYStart);
      var viewRect = new Rect(0, 0, scrollRect.width - 16f, viewHeight);

      Widgets.BeginScrollView(scrollRect, ref scrollPos, viewRect);

      var listingScroll = new Listing_Standard();
      listingScroll.Begin(viewRect);

      for (var i = 0; i < shaderOptions.Count; i++) {
        var shader = shaderOptions[i];
        if (shader == null) continue;

        var label = $"{shader.name} ({i})";
        if (listingScroll.RadioButton(label, i == Settings.activeShaderIndex)) {
          Settings.activeShaderIndex = i;
        }
      }

      listingScroll.End();
      Widgets.EndScrollView();
    }

    public override string SettingsCategory() { return "Growing Zone Icons"; }
  }

  [HarmonyPatch(typeof(MapDrawer), nameof(MapDrawer.DrawMapMesh))]
  public static class PatchZone_CreateMaterial {
    public static void Postfix(MapDrawer __instance) {
      if (!DebugViewSettings.drawWorldOverlays
        || WorldComponent_GravshipController.CutsceneInProgress
        || WorldComponent_GravshipController.GravshipRenderInProgess)
        return;

      if (!OverlayDrawHandler.ShouldDrawZones) return;
      if (GrowingZoneIcons.Settings.drawOnlyWhenSelectedZone
        && Find.Selector.SelectedZone == null)
        return;

      foreach (var zone in Find.CurrentMap.zoneManager.AllZones) {
        if (zone is not Zone_Growing growingZone || zone.Hidden) continue;

        var zoneCenter = (zone.cells.Count == 0 ?
          IntVec3.Invalid :
          zone.cells.Aggregate((current, next) => current + next)
          / zone.cells.Count).ToVector3();

        zoneCenter += GrowingZoneIcons.Settings.drawOffset.ToVector3();

        Log.Message(
          $"Stored: {new Vector2(3f, 3f)} Config: {GrowingZoneIcons.Settings.drawScale}");

        var graphic = growingZone.PlantDefToGrow.graphic;
        graphic = graphic.GetCopy(GrowingZoneIcons.Settings.drawScale,
          GrowingZoneIcons.ActiveShader);

        graphic.DrawFromDef(zoneCenter, Rot4.North, growingZone.PlantDefToGrow);
      }
    }
  }
}