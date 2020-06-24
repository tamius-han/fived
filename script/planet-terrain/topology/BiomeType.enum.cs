using System.Collections.Generic;

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
    public float GetHeightMultiplierForValue(float value) {
      if (value >= blendTo || prev == null) {
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
    public List<LandscapeTypeConfObject> landscapeTypeGradient;

    public LandscapeTypeConf() {
      this.landscapeTypeGradient = new List<LandscapeTypeConfObject>();
    }
    public LandscapeTypeConf(List<LandscapeTypeConfObject> confObjects) {
      this.landscapeTypeGradient = confObjects;
    }

    public void AddTypeConf(LandscapeTypeConfObject confObject) {
      if (this.landscapeTypeGradient.Count > 0) {
        confObject.prev = this.landscapeTypeGradient[this.landscapeTypeGradient.Count - 1];
      }
      this.landscapeTypeGradient.Add(confObject);
    }
    public void AddTypeConf(LandscapeType type, float rarityFrom, float blendDistance, float heightMultiplier) {
      if (this.landscapeTypeGradient.Count > 0) {
        this.landscapeTypeGradient.Add(new LandscapeTypeConfObject(type, rarityFrom, blendDistance, heightMultiplier, this.landscapeTypeGradient[this.landscapeTypeGradient.Count - 1]));
      } else {
        this.landscapeTypeGradient.Add(new LandscapeTypeConfObject(type, rarityFrom, blendDistance, heightMultiplier));
      }
    }

    // get gradient:
    public float GetHeightMultiplierForValue(float value) {
      int i = this.landscapeTypeGradient.Count;

      if (i == 0) {
        return 1.0f;
      }

      // Let's take our gradient list. It goes like this:
      // 
      //      0.0 --> 0.4 --> 0.6 --> 1.0
      // 
      // Our height type multipliers are:
      //       |-[A]-->|-[B]-->|-[C]-->|-[D]-->
      //
      // We get passed a value of 0.5, which belongs to the 'B' segment.
      // This means that we need to return multiplier from the first 
      // segment where <value> becomes greater than rarityFrom

      while (i --> 0) {
        if (value > this.landscapeTypeGradient[i].rarityFrom) {
          return this.landscapeTypeGradient[i].GetHeightMultiplierForValue(value);
        }
      }

      return this.landscapeTypeGradient[0].GetHeightMultiplierForValue(value);
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