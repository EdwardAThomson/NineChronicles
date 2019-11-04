using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public interface ICombinationPanel
    {
        int CostNCG { get; }
        int CostAP { get; }
    }

    public abstract class CombinationPanel<T> : MonoBehaviour, ICombinationPanel
        where T : CombinationPanel<T>
    {
        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        [CanBeNull] public CombinationMaterialView baseMaterial;
        public CombinationMaterialView[] otherMaterials;
        public SubmitWithCostButton submitButton;

        public readonly Subject<InventoryItem> OnMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<int> OnCostNCGChange = new Subject<int>();
        public readonly Subject<int> OnCostAPChange = new Subject<int>();

        public int CostNCG { get; private set; }
        public int CostAP { get; private set; }
        
        public bool IsThereAnyUnlockedEmptyMaterialView { get; private set; }
        public abstract bool IsSubmittable { get; }

        #region Initialize & Terminate

        protected virtual void Awake()
        {
            if (!(baseMaterial is null))
            {
                InitMaterialView(baseMaterial);
            }

            foreach (var otherMaterial in otherMaterials)
            {
                InitMaterialView(otherMaterial);
            }
        }

        protected virtual void OnDestroy()
        {
            OnMaterialAdd.Dispose();
            OnBaseMaterialAdd.Dispose();
            OnOtherMaterialAdd.Dispose();
            OnMaterialRemove.Dispose();
            OnBaseMaterialRemove.Dispose();
            OnOtherMaterialRemove.Dispose();
            OnCostNCGChange.Dispose();
            OnCostAPChange.Dispose();
        }

        private void InitMaterialView(CombinationMaterialView view)
        {
            view.OnClick.Subscribe(_ =>
                {
                    var tooltip = Widget.Find<ItemInformationTooltip>();
                    if (tooltip.Target &&
                        tooltip.Target.Equals(_.RectTransform))
                        tooltip.Close();
                    else
                        tooltip.Show(_.RectTransform, _.Model);
                })
                .AddTo(gameObject);
            view.OnRightClick.Subscribe(_ =>
            {
                Debug.LogWarning("view.OnRightClick!!");
                if (_ is CombinationMaterialView materialView)
                {
                    TryRemoveMaterial(materialView);
                }
            }).AddTo(gameObject);
            view.OnCountChange.Subscribe(_ => OnMaterialCountChanged()).AddTo(gameObject);
        }

        #endregion

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnMaterialAddedOrRemoved();
            OnMaterialCountChanged();
            ReactiveAgentState.Gold.Subscribe(SubscribeNCG).AddTo(_disposablesAtShow);
            ReactiveCurrentAvatarState.ActionPoint.Subscribe(SubscribeActionPoint).AddTo(_disposablesAtShow);
        }

        public virtual void Hide()
        {
            _disposablesAtShow.DisposeAllAndClear();

            RemoveMaterialsAll();

            gameObject.SetActive(false);
        }
        
        public abstract bool DimFunc(InventoryItem inventoryItem);
        
        public virtual bool Contains(InventoryItem inventoryItem)
        {
            if (inventoryItem.ItemBase.Value is ItemUsable itemUsable)
            {
                if (!(baseMaterial is null))
                {
                    if (baseMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                foreach (var otherMaterial in otherMaterials)
                {
                    if (otherMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                return false;
            }

            if (!(baseMaterial is null) &&
                !(baseMaterial.InventoryItemViewModel is null))
            {
                if (baseMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                    inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            foreach (var otherMaterial in otherMaterials)
            {
                if (!(otherMaterial.InventoryItemViewModel is null) &&
                    otherMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                        inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            return false;
        }

        protected abstract int GetCostNCG();
        protected abstract int GetCostAP();

        #region Add Material

        public virtual bool TryAddMaterial(InventoryItemView view)
        {
            if (view is null ||
                view.Model is null ||
                view.Model.Dimmed.Value)
                return false;

            if (TryAddBaseMaterial(view))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialAdd.OnNext(view.Model);
                OnBaseMaterialAdd.OnNext(view.Model);
                return true;
            }

            if (TryAddOtherMaterial(view))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialAdd.OnNext(view.Model);
                OnOtherMaterialAdd.OnNext(view.Model);
                return true;
            }

            return false;
        }

        protected virtual bool TryAddBaseMaterial(InventoryItemView view)
        {
            if (baseMaterial is null)
                return false;

            if (!(baseMaterial.InventoryItemViewModel is null))
            {
                OnMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
                OnBaseMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
            }

            baseMaterial.Set(view);
            return true;
        }

        protected virtual bool TryAddOtherMaterial(InventoryItemView view)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
            {
                // 새로 더하기.
                var possibleMaterial = otherMaterials.FirstOrDefault(e => e.IsEmpty && !e.IsLocked);
                if (possibleMaterial is null)
                {
                    // 제료가 이미 가득 찼어요!
                    return false;
                }

                possibleMaterial.Set(view);
                return true;
            }

            // 하나 증가.
            sameMaterial.IncreaseCount();
            return true;
        }

        #endregion

        #region Remove Material

        public virtual bool TryRemoveMaterial(CombinationMaterialView view)
        {
            if (view is null ||
                view.Model is null)
                return false;

            var inventoryItemView = view.InventoryItemViewModel;

            if (TryRemoveBaseMaterial(view))
            {
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(inventoryItemView);
                OnBaseMaterialRemove.OnNext(inventoryItemView);
                return true;
            }

            if (TryRemoveOtherMaterial(view))
            {
                ReorderOtherMaterials();
                
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(inventoryItemView);
                OnOtherMaterialRemove.OnNext(inventoryItemView);
                return true;
            }

            return false;
        }

        protected virtual bool TryRemoveBaseMaterial(CombinationMaterialView view)
        {
            if (baseMaterial is null ||
                baseMaterial.Model?.ItemBase.Value is null ||
                view is null ||
                view.Model?.ItemBase.Value is null ||
                baseMaterial.Model.ItemBase.Value.Data.Id != view.Model.ItemBase.Value.Data.Id)
                return false;

            baseMaterial.Clear();
            return true;
        }

        protected virtual bool TryRemoveOtherMaterial(CombinationMaterialView view)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
                return false;

            sameMaterial.Clear();
            return true;
        }

        public void RemoveMaterialsAll()
        {
            if (!(baseMaterial is null))
            {
                var model = baseMaterial.InventoryItemViewModel;
                baseMaterial.Clear();
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(model);
                OnBaseMaterialRemove.OnNext(model);
            }

            foreach (var material in otherMaterials)
            {
                var model = material.InventoryItemViewModel;
                material.Clear();
                OnMaterialAddedOrRemoved();
                OnMaterialCountChanged();
                OnMaterialRemove.OnNext(model);
                OnOtherMaterialRemove.OnNext(model);
            }
        }

        #endregion
        
        private void SubscribeNCG(decimal ncg)
        {
            if (CostNCG > 0)
            {
                submitButton.ShowNCG(CostNCG, ncg >= CostNCG);
            }
            else
            {
                submitButton.HideNCG();
            }
        }

        private void SubscribeActionPoint(int ap)
        {
            if (CostAP > 0)
            {
                submitButton.ShowAP(CostAP, ap >= CostAP);
            }
            else
            {
                submitButton.HideAP();
            }
        }

        private void ReorderOtherMaterials()
        {
            for (var i = 0; i < otherMaterials.Length; i++)
            {
                var dstMaterial = otherMaterials[i];
                if (!dstMaterial.IsEmpty)
                    continue;
                
                CombinationMaterialView srcMaterial = null;
                for (var j = i + 1; j < otherMaterials.Length; j++)
                {
                    var tempMaterial = otherMaterials[j];
                    if (tempMaterial.IsEmpty)
                        continue;

                    srcMaterial = tempMaterial;
                    break;
                }

                if (srcMaterial is null)
                    break;
                    
                dstMaterial.Set(srcMaterial.InventoryItemViewModel);
                srcMaterial.Clear();
            }
        }
        
        private void OnMaterialAddedOrRemoved()
        {
            if (!(baseMaterial is null) &&
                baseMaterial.IsEmpty)
            {
                IsThereAnyUnlockedEmptyMaterialView = true;
                return;
            }

            IsThereAnyUnlockedEmptyMaterialView =
                otherMaterials.Any(otherMaterial => !otherMaterial.IsLocked && otherMaterial.IsEmpty);
        }

        private void OnMaterialCountChanged()
        {
            CostNCG = GetCostNCG();
            SubscribeNCG(ReactiveAgentState.Gold.Value);
            CostAP = GetCostAP();
            SubscribeActionPoint(ReactiveCurrentAvatarState.ActionPoint.Value);
            OnCostAPChange.OnNext(CostAP);
            UpdateSubmittable();
        }

        private void UpdateSubmittable()
        {
            submitButton.button.interactable = IsSubmittable;
        }
    }
}