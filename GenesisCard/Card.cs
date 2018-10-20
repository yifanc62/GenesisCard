namespace GenesisCard {
    public class Card {
        public byte Version {get; set;}
        public Volume Volume {get; set;}
        public byte VolumeId {get; set;}
        public byte Rarity {get; set;}
        public string Texture {get; set;}
        public string Title {get; set;}
        public string Illustrator {get; set;}
        public byte Copyright {get; set;}
        public ushort Year {get; set;}
        public byte Frame {get; set;}
        public short Bright {get; set;}

        public string GetId() {
            return $"SV{Version:D2}-{GetTypeChar()}{Volume.Index:D3}-{VolumeId:D3}/{Volume.MaxId:D3}";
        }

        private char GetTypeChar() {
            return Volume.Type == 0 ? 'N' : (Volume.Type == 1 ? 'S' : '?');
        }
    }
}