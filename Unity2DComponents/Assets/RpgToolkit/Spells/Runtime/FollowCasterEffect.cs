using UnityEngine;

namespace RpgToolkit.Spells
{
    [CreateAssetMenu(menuName = "RPG Toolkit/Spells/Effect/Follow")]
    public sealed class FollowCasterEffect : SpellEffect
    {

        public override void Apply(SpellEvaluationContext context)
        {
            context.Position  = context.Caster.transform.position;
            
        }
    }
}


    