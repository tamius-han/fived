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

  public class LandscapeTypeConfObject {
    public LandscapeType type;
    public float rarityFrom; // cumulative!
    public float blendDistance;
    public float blendTo;
    public float heightMultiplier;
    public LandscapeTypeConfObject prev;

    public LandscapeTypeConfObject(LandscapeType type, float rarityFrom, float blendDistance, float heightMultiplier) {
      this.type = type;
      this.rarityFrom = rarityFrom;
      this.blendDistance = blendDistance;
      this.blendTo = rarityFrom + blendDistance;
      this.heightMultiplier = heightMultiplier;
    }
    
    public LandscapeTypeConfObject(LandscapeType type, float rarityFrom, float blendDistance, float heightMultiplier, LandscapeTypeConfObject prev) : this(type, rarityFrom, blendDistance, heightMultiplier) {
      this.prev = prev;
    }

    // Landscape types are determined by an output of 3D perlin function that takes 3d coordinate
    // of planet vertex and throws out something. By default, the something is on [-1, 1] but in
    // practice this can be changed (this project has been scaling output of perlin to [0, 1] and
    // other ranges at different points in time).
    // 
    // rarityFrom and blendDistance and blendTo are supposed to be the parameters that help determine
    // the output of this function.
    //
    // perlin_min                                                                           perlin_max 
    // :                                                                                             :
    // +——————————————————————————×———×——————————————————————————————————————————————————————————————+
    //                            |   |               
    //                           [a] [b]             
    //
    // [a] — this is rarityFrom. Anything bigger than this belongs to this LandscapeType. Anything
    //       smaller belongs to the previous LandscapeType.
    // [b] — this is blendTo. It's calculated automatically from blendDistance parameter.
    //       blendDistance is the length between points a and b and must be provided.
    // 
    // When getting height multiplier, we determine if our perlin value is greater than blendTo. 
    // If it is, we return heightMultiplier as-is. If it's between points a and b, we return 
    // heightMultiplier as-is only if this.prev is missing. If this.prev is present, we calculate
    // our heightMultiplier depending on where on the blendDistance line we are. Closer to 
    // rarityFrom, the more weight heightMultiplier from previous LandscapeType will have.
    //
    // The value of value should never be less than rarityFrom. This function doesn't check whether
    // that's correct — checks like that need to be implemented elsewhere, possibly in LandscapeTypeConf.
    public float getHeightMultiplierForValue(float value) {
      if (value > blendTo || prev == null) {
        return this.heightMultiplier;
      } else {
        // Move value from [rarityFrom, blendTo] to [0, 1] for convenience
        float blendPercent = (value - this.rarityFrom) / this.blendDistance;

        // yes we're doing a linear blend. Let's not be too sinful.
        return (this.heightMultiplier * blendPercent) + (this.prev.heightMultiplier * (1 - blendPercent));
      }  
    }
  }

  public class LandscapeTypeConf {

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