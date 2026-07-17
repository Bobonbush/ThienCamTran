using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface SpecialSkill
{
    public  bool Trigger();
    public float GetMaxCoolDown();
    public float GetCoolDown();
    public int GetManaCost();
    public string GetAnimationString();

    // Cue id trong SfxLibrary phát lúc ra chiêu — mỗi skill một chất tiếng riêng
    public string GetCastSfxId();

    public Sprite GetSprite();
}

