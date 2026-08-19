using System;
using System.Collections.Generic;
using Frenil.MVVM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDemo.UI
{
    /// <summary>
    /// 강화 리포트 팝업. 열림 여부와 내용은 ViewModel이 정하고, 이 View는 그리기만 한다.
    ///
    /// 닫는 방법이 둘(X 버튼, 팝업 바깥 클릭)이지만 둘 다 ViewModel의 같은 메서드로 들어간다.
    /// 닫기 규칙이 View에 흩어지면 "어떤 경로로 닫으면 리포트가 안 지워진다" 같은 버그가 생긴다.
    ///
    /// 바깥 클릭은 전체 화면을 덮는 딤 이미지에 Button을 달아 처리한다. 딤이 팝업 패널보다
    /// 뒤에 있으므로 패널 위를 눌렀을 때는 딤의 클릭이 발생하지 않는다.
    /// </summary>
    public sealed class RewardPopupView : MonoBehaviour
    {
        [Tooltip("팝업 전체(딤 + 패널)를 담는 오브젝트. 열림/닫힘에 따라 켜고 끈다")]
        [SerializeField] private GameObject _container;

        [SerializeField] private Button _closeButton;

        [Tooltip("팝업 바깥 영역. 클릭하면 닫힌다")]
        [SerializeField] private Button _dimmerButton;

        [Tooltip("리포트 행이 추가될 스크롤 콘텐츠")]
        [SerializeField] private RectTransform _listRoot;

        [Tooltip("행 하나의 템플릿. 비활성 상태로 두고 복제해서 쓴다")]
        [SerializeField] private TextMeshProUGUI _rowTemplate;

        private readonly List<TextMeshProUGUI> _rows = new List<TextMeshProUGUI>();
        private readonly List<Action> _unbindActions = new List<Action>();

        private LobbyViewModel _viewModel;

        public void Bind(LobbyViewModel viewModel)
        {
            Unbind();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            _rowTemplate.gameObject.SetActive(false);

            Subscribe(viewModel.IsRewardPopupOpen, open => _container.SetActive(open));
            Subscribe(viewModel.Reports, OnReportsChanged);

            _closeButton.onClick.AddListener(viewModel.CloseRewardPopup);
            _dimmerButton.onClick.AddListener(viewModel.CloseRewardPopup);
        }

        public void Unbind()
        {
            foreach (var unbind in _unbindActions)
                unbind?.Invoke();

            _unbindActions.Clear();

            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_dimmerButton != null) _dimmerButton.onClick.RemoveAllListeners();

            _viewModel = null;
        }

        private void OnReportsChanged(ListChange<string> change)
        {
            switch (change.Type)
            {
                case ListChangeType.Added:
                    InsertRow(change.Index, change.NewItem);
                    break;

                case ListChangeType.Removed:
                    RemoveRow(change.Index);
                    break;

                case ListChangeType.Replaced:
                    _rows[change.Index].text = change.NewItem;
                    break;

                default:
                    RebuildAll();
                    break;
            }
        }

        private void InsertRow(int index, string text)
        {
            var row = Instantiate(_rowTemplate, _listRoot);
            row.gameObject.SetActive(true);
            row.text = text;
            row.transform.SetSiblingIndex(index);
            _rows.Insert(index, row);
        }

        private void RemoveRow(int index)
        {
            var row = _rows[index];
            _rows.RemoveAt(index);
            Destroy(row.gameObject);
        }

        private void RebuildAll()
        {
            for (int i = _rows.Count - 1; i >= 0; i--)
                RemoveRow(i);

            if (_viewModel == null) return;

            var reports = _viewModel.Reports;
            for (int i = 0; i < reports.Count; i++)
                InsertRow(i, reports[i]);
        }

        // ViewBase.Subscribe와 같은 규칙 - 구독 즉시 현재 상태로 1회 동기화한다.
        private void Subscribe<T>(IReadOnlyObservable<T> observable, Action<T> handler)
        {
            observable.OnChanged += handler;
            _unbindActions.Add(() => observable.OnChanged -= handler);
            handler(observable.Value);
        }

        private void Subscribe<T>(IReadOnlyObservableList<T> list, Action<ListChange<T>> handler)
        {
            list.OnChanged += handler;
            _unbindActions.Add(() => list.OnChanged -= handler);
            handler(ListChange<T>.Reset());
        }

        private void OnDestroy() => Unbind();
    }
}
