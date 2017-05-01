﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Sledge.BspEditor.Modification;
using Sledge.BspEditor.Primitives.MapObjects;
using Sledge.Common.Transport;
using Sledge.DataStructures.Geometric;

namespace Sledge.BspEditor.Primitives.MapData
{
    public class Selection : IMapData, IEnumerable<IMapObject>
    {
        private readonly HashSet<IMapObject> _selectedObjects;

        public Selection()
        {
            _selectedObjects = new HashSet<IMapObject>();
        }

        public Selection(SerialisedObject obj)
        {

        }

        [Export(typeof(IMapElementFormatter))]
        public class SelectionFormatter : StandardMapElementFormatter<Selection> { }

        public bool IsEmpty => _selectedObjects.Count == 0;
        public int Count => _selectedObjects.Count;

        /// <summary>
        /// Update the selection based on a change.
        /// </summary>
        /// <param name="change"></param>
        /// <returns>True if the selection has changed after updating.</returns>
        public bool Update(Change change)
        {
            var changed = false;

            // Each of these operations is only adding or removing items,
            // so checking the count to detect changes is fine.

            var c = _selectedObjects.Count;
            _selectedObjects.UnionWith(change.Added.Where(x => x.IsSelected));
            _selectedObjects.UnionWith(change.Updated.Where(x => x.IsSelected));
            changed |= c != _selectedObjects.Count;

            c = _selectedObjects.Count;
            _selectedObjects.ExceptWith(change.Added.Where(x => !x.IsSelected));
            _selectedObjects.ExceptWith(change.Updated.Where(x => !x.IsSelected));
            _selectedObjects.ExceptWith(change.Removed);
            changed |= c != _selectedObjects.Count;

            return changed;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Nothing.
        }

        public IMapData Clone()
        {
            var c = new Selection();
            c._selectedObjects.UnionWith(_selectedObjects);
            return c;
        }

        public SerialisedObject ToSerialisedObject()
        {
            var so = new SerialisedObject("Selection");
            so.Set("SelectedObjects", String.Join(",", _selectedObjects.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            return so;
        }

        public IEnumerator<IMapObject> GetEnumerator()
        {
            return _selectedObjects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Box GetSelectionBoundingBox()
        {
            return IsEmpty ? Box.Empty : new Box(_selectedObjects.Select(x => x.BoundingBox).Where(x => x != null).DefaultIfEmpty(Box.Empty));
        }

        public IEnumerable<IMapObject> GetSelectedParents()
        {
            var sel = _selectedObjects.ToList();
            sel.SelectMany(x => x.Hierarchy).ToList().ForEach(x => sel.Remove(x));
            return sel;
        }
    }
}
