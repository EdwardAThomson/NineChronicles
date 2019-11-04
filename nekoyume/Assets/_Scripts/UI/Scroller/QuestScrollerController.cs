﻿using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UniRx;
using System.Collections.Generic;

namespace Nekoyume.UI.Scroller
{
    public class QuestScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        public EnhancedScroller scroller;
        public QuestCellView cellViewPrefab;
        public readonly Subject<MailCellView> onClickCellView = new Subject<MailCellView>();

        private List<Game.Quest.Quest> _data;
        private float _cellViewHeight = 40f;

        #region Mono

        private void Awake()
        {
            scroller.Delegate = this;
            _cellViewHeight = cellViewPrefab.GetComponent<RectTransform>().rect.height;
        }

        #endregion

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = scroller.GetCellView(cellViewPrefab) as QuestCellView;
            if (cellView is null)
            {
                throw new FailedToInstantiateGameObjectException(cellViewPrefab.name);
            }

            cellView.name = $"Cell {dataIndex}";
            cellView.SetData(_data[dataIndex]);
            return cellView;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return _cellViewHeight;
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return _data.Count;
        }

        public void SetData(List<Game.Quest.Quest> dataList)
        {
            _data = dataList;
            scroller.ReloadData();
        }
    }
}