using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using RoR2;
using RoR2.Skills;
using EntityStates;
using R2API;
using R2API.Utils;
using ArtificerExtended.Unlocks;

namespace ArtificerExtended.Passive
{
    public class PassiveSkillDef : SkillDef
    {
        public struct StateMachineDefaults
        {
            public String machineName;
            public SerializableEntityStateType initalState;
            public SerializableEntityStateType mainState;
            public SerializableEntityStateType defaultInitalState;
            public SerializableEntityStateType defaultMainState;
        }

        public StateMachineDefaults[] stateMachineDefaults;

        public override SkillDef.BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
        {
            EntityStateMachine[] stateMachines = skillSlot.GetComponents<EntityStateMachine>();
            foreach (StateMachineDefaults def in this.stateMachineDefaults)
            {
                foreach (EntityStateMachine mach in stateMachines)
                {
                    if (mach.customName == def.machineName)
                    {
                        mach.initialStateType = def.initalState;
                        mach.mainStateType = def.mainState;

                        if (mach.state.GetType() == def.defaultMainState.stateType)
                        {
                            Type sest = def.mainState.stateType;
                            mach.SetNextState(EntityStateCatalog.InstantiateState(EntityStateCatalog.GetStateIndex(sest)));
                        }

                        break;
                    }
                }
            }

            return base.OnAssigned(skillSlot);
        }

        public override void OnUnassigned([NotNull] GenericSkill skillSlot)
        {
            EntityStateMachine[] stateMachines = skillSlot.GetComponents<EntityStateMachine>();
            foreach (StateMachineDefaults def in this.stateMachineDefaults)
            {
                foreach (EntityStateMachine mach in stateMachines)
                {
                    if (mach.customName == def.machineName)
                    {
                        mach.initialStateType = def.defaultInitalState;
                        mach.mainStateType = def.defaultMainState;

                        if (mach.state.GetType() == def.mainState.stateType)
                        {
                            Type sest = def.defaultMainState.stateType;
                            mach.SetNextState(EntityStateCatalog.InstantiateState(EntityStateCatalog.GetStateIndex(sest)));
                        }

                        break;
                    }
                }
            }

            base.OnUnassigned(skillSlot);
        }

        internal UnlockableDef GetUnlockDef(Type type)
        {
            UnlockableDef u = null;

            foreach (KeyValuePair<UnlockBase, UnlockableDef> keyValuePair in ArtificerExtendedPlugin.UnlockBaseDictionary)
            {
                string key = keyValuePair.Key.ToString();
                UnlockableDef value = keyValuePair.Value;
                if (key == type.ToString())
                {
                    u = value;
                    //Debug.Log($"Found an Unlock ID Match {value} for {type.Name}! ");
                    break;
                }
            }

            return u;
        }
    }
}