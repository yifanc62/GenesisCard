using System;
using System.Collections.Generic;

namespace GenesisCard {
    public class Volume {
        public byte Type {get; set;}
        public byte Index {get; set;}
        public byte MaxId {get; set;}

        private static readonly Dictionary<int, Volume> VolumeDict = new Dictionary<int, Volume>();

        public static Volume GetVolume(byte type, byte index, byte id) {
            var key = GetKey(type, index);
            Volume volume;
            if (VolumeDict.ContainsKey(key)) {
                volume = VolumeDict[key];
                volume.MaxId = Math.Max(volume.MaxId, id);
            } else {
                volume = new Volume {Type = type, Index = index, MaxId = id};
                VolumeDict.Add(key, volume);
            }
            return volume;
        }

        private static int GetKey(byte type, byte index) {
            return type * 256 + index;
        }
    }
}