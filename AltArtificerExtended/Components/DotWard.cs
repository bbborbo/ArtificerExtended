using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Components
{
    class DotWard : BuffWard
    {
        public DotController.DotIndex dotIndex = DotController.DotIndex.None;
        public float damageCoefficient = 1f;

        ProjectileController _projectileController;
        public ProjectileController projectileController
        {
            get
            {
                if (_projectileController == null)
                {
                    _projectileController = GetComponent<ProjectileController>();
                }
                return _projectileController;
            }
            set
            {
                _projectileController = value;
            }
        }

        GameObject _ownerObject;
        public GameObject ownerObject
        {
            get
            {
                if(_ownerObject == null)
                {
                    _ownerObject = projectileController.owner;
                }
                return _ownerObject;
            }
            set
            {
                _ownerObject = value;
            }
        }

        CharacterBody _ownerBody;
        public CharacterBody ownerBody
        {
            get
            {
                if(_ownerBody == null)
                {
                    _ownerBody = ownerObject.GetComponent<CharacterBody>();
                }
                return _ownerBody;
            }
            set
            {
                _ownerBody = value;
            }
        }

        Inventory _ownerInventory;
        public Inventory ownerInventory
        {
            get
            {
                if(_ownerInventory == null)
                {
                    _ownerInventory = ownerBody.inventory;
                }
                return _ownerInventory;
            }
            set
            {
                _ownerInventory = value;
            }
        }
    }
}
