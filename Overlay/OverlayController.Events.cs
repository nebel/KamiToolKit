using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;

namespace KamiToolKit.Overlay;

public abstract unsafe partial class OverlayController {

    private void AddOverlays() {
        Log.Error($"{GetType()} AddOverlays");
        foreach (var overlayLayer in Enum.GetValues<OverlayLayer>()) {
            var addonName = overlayLayer.GetDescription();

            DalamudInterface.Instance.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, addonName, OnOverlayAddonSetup);
            DalamudInterface.Instance.AddonLifecycle.RegisterListener(AddonEvent.PreUpdate, addonName, OnOverlayAddonUpdate);
            DalamudInterface.Instance.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, addonName, OnOverlayAddonFinalize);

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);
            if (addon is not null) {
                overlayAddons.TryAdd(overlayLayer, addon);
            }
            else {
                var overlayAddon = CreateOverlayAddon(overlayLayer);
                overlayAddon.Open();
                // OnOverlayAddonSetupFake(overlayAddon); // SUPER HACK
            }
        }

        OverlaysActive = true;

        OnOverlaysCreated();
    }

    private void OnOverlayAddonSetupFake(OverlayAddon overlayAddon) {
        Log.Error($"!!2 {GetType()}OnOverlayAddonSetup {overlayAddon.InternalName}");
        var addon = overlayAddon.InternalAddon;
        var overlayLayer = addon->DepthLayer.GetOverlayLayer();

        overlayAddons.TryAdd(overlayLayer, addon);
        AttachNodes(overlayLayer);
    }

    private void RemoveOverlays() {
        Log.Error($"{GetType()} RemoveOverlays");
        DalamudInterface.Instance.AddonLifecycle.UnregisterListener(OnOverlayAddonFinalize, OnOverlayAddonSetup, OnOverlayAddonUpdate);

        foreach (var overlayLayer in Enum.GetValues<OverlayLayer>()) {
            if (overlayNodes.TryGetValue(overlayLayer, out var list)) {
                foreach (var node in list) {
                    node.DetachNode();
                }
            }
        }

        overlayAddons.Clear();
        OverlaysActive = false;

        OnOverlaysRemoved();
    }

    private static OverlayAddon CreateOverlayAddon(OverlayLayer layer) => new() {
        Title = layer.GetDescription(),
        InternalName = layer.GetDescription(),
        DepthLayer = layer.GetOverlayLayer(),
        IsOverlayAddon = true,
    };

    private void OnOverlayAddonSetup(AddonEvent type, AddonArgs args) {
        Log.Error($"!!! {GetType()}OnOverlayAddonSetup {args.Addon.Name}");
        var addon = (AtkUnitBase*)args.Addon.Address;
        var overlayLayer = addon->DepthLayer.GetOverlayLayer();

        overlayAddons.TryAdd(overlayLayer, addon);
        AttachNodes(overlayLayer);
    }

    private void OnOverlayAddonUpdate(AddonEvent type, AddonArgs args) {
        var addon = (AtkUnitBase*)args.Addon.Address;
        var overlayLayer = addon->DepthLayer.GetOverlayLayer();

        if (overlayNodes.TryGetValue(overlayLayer, out var list)) {
            foreach (var node in list) {
                node.Update();
            }
        }
    }

    private void OnOverlayAddonFinalize(AddonEvent type, AddonArgs args) {
        Log.Error($"!!! {GetType()} OnOverlayAddonFinalize {args.Addon.Name}");
        var addon = (AtkUnitBase*)args.Addon.Address;
        var overlayLayer = addon->DepthLayer.GetOverlayLayer();
        
        overlayAddons.Remove(overlayLayer);
        DetachNodes(overlayLayer);
    }

    private void AttachNodes(OverlayLayer layer) {
        Log.Error($"{GetType()} AttachNodes {layer}");
        if (!overlayAddons.TryGetValue(layer, out var addon)) return;
        if (!overlayNodes.TryGetValue(layer, out var list)) return;

        foreach (var node in list) {
            node.NodeId = (uint)addon.Value->UldManager.NodeListCount + 1;
            node.AttachNode(addon);
        }
    }

    private void DetachNodes(OverlayLayer layer) {
        Log.Error($"{GetType()} DetachNodes {layer}");
        if (!overlayNodes.TryGetValue(layer, out var list)) return;

        foreach (var node in list) {
            node.DetachNode();
        }
    }

    protected abstract void OnOverlaysCreated();

    protected abstract void OnOverlaysRemoved();
}
