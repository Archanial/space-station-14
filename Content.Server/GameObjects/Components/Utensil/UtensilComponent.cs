﻿using System.Collections.Generic;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Utensil
{
    [RegisterComponent]
    public class UtensilComponent : SharedUtensilComponent, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystem;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        protected UtensilType _types = UtensilType.None;

        [ViewVariables]
        public override UtensilType Types
        {
            get => _types;
            set
            {
                _types = value;
                Dirty();
            }
        }

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables]
        private float _breakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables]
        private string _breakSound;

        public void AddType(UtensilType type)
        {
            Types |= type;
        }

        public bool HasAnyType(UtensilType type)
        {
            return (_types & type) != UtensilType.None;
        }

        public bool HasType(UtensilType type)
        {
            return _types.HasFlag(type);
        }

        public void RemoveType(UtensilType type)
        {
            Types &= ~type;
        }

        internal void TryBreak(IEntity user)
        {
            if (_random.Prob(_breakChance))
            {
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(_breakSound, user, AudioParams.Default.WithVolume(-2f));
                Owner.Delete();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                var types = serializer.ReadDataField("types", new List<UtensilType>());
                foreach (var type in types)
                {
                    AddType(type);
                }
            }

            serializer.DataField(ref _breakChance, "breakChance", 0);
            serializer.DataField(ref _breakSound, "breakSound", "/Audio/Items/snap.ogg");
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryUseUtensil(eventArgs.User, eventArgs.Target);
        }

        private void TryUseUtensil(IEntity user, IEntity target)
        {
            if (user == null || target == null)
            {
                return;
            }

            if (!target.TryGetComponent(out FoodComponent food))
            {
                return;
            }

            if (!InteractionChecks.InRangeUnobstructed(user, target.Transform.MapPosition))
            {
                return;
            }

            food.TryUseFood(user, null, this);
        }
    }
}
