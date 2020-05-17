namespace Biome {
  public enum LandscapeType {
    Mountains = 1,
    Hills = 2,
    Plains = 3,
    Swamps = 4,
    Desert = 5
  }

  public enum RockType {
    Hard = 0,
    Soft = 1,
    Karst = 2
  }

  public class LandscapeTypeConf {
    public LandscapeType type;
    public float rarityFrom; // cummulative!
    public float rarityTo;

    public LandscapeTypeConf(LandscapeType type, float rarityFrom, float rarityTo) {
      this.type = type;
      this.rarityFrom = rarityFrom;
      this.rarityTo = rarityTo;
    }
  }

  public class CellLandscapeType {
    public LandscapeType type;
    public float score;

    public CellLandscapeType(LandscapeType type, float score) {
      this.type = type;
      this.score = score;
    }
  }
}