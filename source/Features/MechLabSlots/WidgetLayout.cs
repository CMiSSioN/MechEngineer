﻿using BattleTech.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MechEngineer.Features.MechLabSlots
{
    internal class WidgetLayout
    {
        internal WidgetLayout(MechLabLocationWidget widget)
        {
            this.widget = widget;
            layout_slots = widget.transform.GetChild("layout_slots");
            slots = layout_slots.GetChildren()
                .Where(x => x.name.StartsWith("slot"))
                .ToList();
        }

        internal MechLabLocationWidget widget { get; }

        internal Transform layout_slots { get; }

        internal List<Transform> slots { get; }
    }
}