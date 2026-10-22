using System;
using System.Collections.Generic;

namespace WhipItUp.Ui
{
    /// <summary>
    /// Which weapon the menu shows: a family + a rank, shared by the viewer tab and the details tab (D-048 Q2). Pure logic, no Unity / Valheim
    /// types, unit-tested (WeaponSelectionTests). Changed fires once per real change and never when the same weapon is set again.
    /// Logging goes through the injected `log` (the menu passes UiLog.Verbose; null = silent), so this file also compiles in the test project.
    /// </summary>
    internal sealed class WeaponSelection
    {
        /// The weapon families in the order the drop-down lists them.
        internal static readonly string[] Families = { "Paddle", "Flogger", "Whip" };

        readonly List<WeaponInfo> _weapons;
        readonly Action<string> _log;
        string _family;
        int _rank;

        /// Raised after the family and / or rank changed (once per change).
        internal event Action Changed;

        internal string Family { get { return _family; } }
        internal int Rank { get { return _rank; } }
        /// The selected weapon, null when the list has no such weapon (an empty list).
        internal WeaponInfo Current { get { return Find(_family, _rank); } }

        /// Starts on the first weapon of `weapons` (Paddle V1 in the normal list); an empty or null list gives Paddle V1 with no weapon data.
        internal WeaponSelection(IList<WeaponInfo> weapons, Action<string> log = null)
        {
            _weapons = weapons != null ? new List<WeaponInfo>(weapons) : new List<WeaponInfo>();
            _log = log;
            var first = _weapons.Count > 0 ? _weapons[0] : null;
            _family = first != null ? first.Family : Families[0];
            _rank = first != null ? first.Rank : 1;
            Log("WeaponSelection: " + _weapons.Count + " weapon(s), start " + _family + " V" + _rank);
        }

        /// The weapon of that family and rank, null when there is none.
        internal WeaponInfo Find(string family, int rank)
        {
            foreach (var w in _weapons)
                if (w.Rank == rank && string.Equals(w.Family, family, StringComparison.Ordinal)) return w;
            return null;
        }

        /// The weapons of one family, weakest rank first.
        internal List<WeaponInfo> InFamily(string family)
        {
            var list = new List<WeaponInfo>();
            foreach (var w in _weapons)
                if (string.Equals(w.Family, family, StringComparison.Ordinal)) list.Add(w);
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            return list;
        }

        /// Selects that weapon. Equal to the current one: nothing happens, no event. Unknown weapon: ignored (logged).
        /// Expected outcome: returns whether the selection IS that weapon afterwards.
        internal bool Set(string family, int rank)
        {
            if (string.Equals(family, _family, StringComparison.Ordinal) && rank == _rank)
            {
                Log("WeaponSelection: " + family + " V" + rank + " is already selected, no change");
                return _family == family && _rank == rank;
            }
            if (Find(family, rank) == null)
            {
                Log("WeaponSelection: no weapon " + family + " V" + rank + " in the list of " + _weapons.Count + ", selection stays " + _family + " V" + _rank);
                return _family == family && _rank == rank;
            }
            Log("WeaponSelection: " + _family + " V" + _rank + " -> " + family + " V" + rank);
            _family = family;
            _rank = rank;
            if (Changed != null) Changed();
            return _family == family && _rank == rank;
        }

        /// Another family, same rank (the rank is kept when the new family has it, else the weakest one).
        internal bool SetFamily(string family)
        {
            if (Find(family, _rank) != null) return Set(family, _rank);
            var inFamily = InFamily(family);
            if (inFamily.Count == 0)
            {
                Log("WeaponSelection: family '" + family + "' has no weapons, selection stays " + _family + " V" + _rank);
                return _family == family;
            }
            Log("WeaponSelection: " + family + " has no V" + _rank + ", taking its weakest V" + inFamily[0].Rank);
            return Set(family, inFamily[0].Rank);
        }

        /// Another rank, same family.
        internal bool SetRank(int rank) { return Set(_family, rank); }

        void Log(string s) { if (_log != null) _log(s); }
    }
}
