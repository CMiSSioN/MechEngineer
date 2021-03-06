﻿using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CustomComponents;
using MechEngineer.Features.MechLabSlots;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MechEngineer.Features.DynamicSlots
{
    internal class DynamicSlotsFeature : Feature<DynamicSlotsSettings>, IValidateMech
    {
        internal static DynamicSlotsFeature Shared = new DynamicSlotsFeature();

        internal override DynamicSlotsSettings Settings => Control.settings.DynamicSlots;

        internal static DynamicSlotsSettings settings => Shared.Settings;

        internal override void SetupFeatureLoaded()
        {
            Validator.RegisterMechValidator(CCValidation.ValidateMech, CCValidation.ValidateMechCanBeFielded);
            if (settings.DynamicSlotsValidateDropEnabled)
            {
                Validator.RegisterDropValidator(check: CCValidation.ValidateDrop);
            }
        }

        internal CCValidationAdapter CCValidation;

        private DynamicSlotsFeature()
        {
            CCValidation = new CCValidationAdapter(this);
        }

        private class DynamicSlotBuilder: IComparable<DynamicSlotBuilder>
        {
            private readonly MechDefBuilder builder;
            private readonly ChassisLocations location;
            private readonly int finalTotalFree;
            private readonly int maxSlots;
            internal readonly MechLabLocationWidgetAdapter adapter;
            internal int currentFreeSlots;

            internal DynamicSlotBuilder(
                MechDefBuilder builder,
                ChassisLocations location,
                MechLabLocationWidgetAdapter adapter)
            {
                this.builder = builder;
                this.location = location;
                this.adapter = adapter;

                currentFreeSlots = builder.GetFreeSlots(location);

                finalTotalFree = this.builder.TotalFree;
                maxSlots = adapter.maxSlots;
            }

            int IComparable<DynamicSlotBuilder>.CompareTo(DynamicSlotBuilder other)
            {
                var diff = currentFreeSlots - other.currentFreeSlots;
                if (diff != 0)
                {
                    return diff;
                }
                var index = Array.IndexOf(settings.LocationPriorityOrder, location);
                var otherIndex = Array.IndexOf(settings.LocationPriorityOrder, other.location);
                return -index.CompareTo(otherIndex);
            }
            
            internal bool currentFreeSlotFixed => currentFreeSlots > finalTotalFree;

            internal int currentFreeSlotIndex
            {
                get
                {
                    var index = maxSlots - currentFreeSlots;
                    if (location == ChassisLocations.CenterTorso)
                    {
                        index -= MechLabSlotsFeature.settings.MechLabGeneralSlots;
                    }
                    return index;
                }
            }
        }

        internal void RefreshData(MechLabPanel mechLab)
        {
            var builder = new MechDefBuilder(mechLab.activeMechDef);

            var fslList = new List<DynamicSlotBuilder>();
            foreach (var location in MechDefBuilder.Locations)
            {
                 // armorlocation = chassislocation for main locations
                var widget = mechLab.GetLocationWidget((ArmorLocation)location);
                ClearFillers(widget);
                var adapter = new MechLabLocationWidgetAdapter(widget);
                fslList.Add(new DynamicSlotBuilder(builder, location, adapter));
            }

            //for (var reservedSlots = builder.Reserved; reservedSlots > 0; reservedSlots--)
            foreach (var reservedSlot in builder.GetReservedSlots())
            {
                var fsl = fslList.Max();
                if (fsl.currentFreeSlots <= 0)
                {
                    // no more free slots to use up!
                    break;
                }
                ShowFiller(fsl.adapter.instance, reservedSlot, fsl.currentFreeSlotIndex, fsl.currentFreeSlotFixed);
                fsl.currentFreeSlots--;
            }
        }

        public void ValidateMech(MechDef mechDef, Errors errors)
        {
            var slots = new MechDefBuilder(mechDef);
            var missing = slots.TotalMissing;
            if (missing > 0)
            {
                errors.Add(MechValidationType.InvalidInventorySlots, $"RESERVED SLOTS: Mech requires {missing} additional free slots");
            }
        }

        internal static void PrepareWidget(WidgetLayout widgetLayout)
        {
            AddFillersToSlots(widgetLayout);
        }

        internal static void ClearFillers(MechLabLocationWidget widget)
        {
            foreach (var filler in Fillers[widget.loadout.Location])
            {
                filler.Hide();
            }
        }

        internal static void ShowFiller(MechLabLocationWidget widget, DynamicSlots slots, int slotIndex, bool isReservedSlot)
        {
            ChassisLocations location = widget.loadout.Location;
            Fillers[location][slotIndex].Show(slots, isReservedSlot);
        }

        private static Dictionary<ChassisLocations, List<Filler>> Fillers = new Dictionary<ChassisLocations, List<Filler>>();

        private static void AddFillersToSlots(WidgetLayout layout)
        {
            var location = layout.widget.loadout.Location;
            // dispose of all fillers
            if (Fillers.TryGetValue(location, out var existing))
            {
                foreach (var old in existing)
                {
                    old.Dispose();
                }
            }

            var fillers = new List<Filler>();
            foreach (var slot in layout.slots)
            {
                fillers.Add(new Filler(slot));
            }

            Fillers[location] = fillers;
        }

        private class Filler : IDisposable
        {
            private readonly GameObject gameObject;
            private readonly MechLabItemSlotElement element;
            private readonly RectTransform backgroundsRect;

            internal void Show(DynamicSlots slots, bool isReservedSlot)
            {
                var dataManager = UnityGameInstance.BattleTechGame.DataManager;
                var id = slots.Def.Description.Id;
                var type = slots.Def.ComponentType;
                var @ref = new MechComponentRef(id, null, type, ChassisLocations.None) {DataManager = dataManager};
                @ref.RefreshComponentDef();
                element.SetData(@ref, ChassisLocations.None, dataManager, null);

                slots.ApplyTo(element, isReservedSlot);

                if (isReservedSlot)
                {
                    ResetSolidBorder();
                }
                else
                {
                    HideSolidBorder();
                }

                gameObject.SetActive(true);
                element.SetDraggable(false);
            }

            internal void Hide()
            {
                ResetSolidBorder();
                gameObject.SetActive(false);
            }

            private void HideSolidBorder()
            {
                backgroundsRect.offsetMin = new Vector2(1, 1);
                backgroundsRect.offsetMax = new Vector2(-1, -1);
            }
            private void ResetSolidBorder()
            {
                backgroundsRect.offsetMin = new Vector2(0, 0);
                backgroundsRect.offsetMax = new Vector2(0, 0);
            }

            public void Dispose()
            {
                // could be null if the pool was cleared and the GameObjects were already destroyed
                // can happen if DataManager.Clear was called
                if (gameObject == null || backgroundsRect == null)
                {
                    return;
                }
                ResetSolidBorder();
                DataManager.PoolGameObject(MechLabPanel.MECHCOMPONENT_ITEM_PREFAB, gameObject);
            }

            internal Filler(Transform parent)
            {
                gameObject = DataManager.PooledInstantiate(
                    MechLabPanel.MECHCOMPONENT_ITEM_PREFAB,
                    BTLoadUtils.GetResourceType(nameof(BattleTechResourceType.UIModulePrefabs)),
                    null, null);


                {
                    element = gameObject.GetComponent<MechLabItemSlotElement>();
                    element.AllowDrag = false;
                    {
                        var rect = gameObject.GetComponent<RectTransform>();
                        rect.pivot = new Vector2(0, 1);
                        rect.anchorMin = new Vector2(0, 0);
                        rect.anchorMax = new Vector2(1, 1);
                        rect.anchoredPosition = Vector2.zero;
                    }
                    element.transform.SetParent(parent, false);
                }

                {
                    var rep = gameObject.transform.GetChild("Representation");
                    var layout_components = rep.GetChild("layout_component");
                    var backgrounds = layout_components.GetChild("BACKGROUNDS");
                    backgroundsRect = backgrounds.GetComponent<RectTransform>();
                }
                Hide();
            }

            private static DataManager DataManager => UnityGameInstance.BattleTechGame.DataManager;
        }
    }
}