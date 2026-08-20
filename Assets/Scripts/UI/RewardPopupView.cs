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
    /// LobbyView와 **같은 ViewModel을 공유**하므로 주입받는다. 소유하지 않으므로
    /// (기본값 takeOwnership: false) 팝업이 파괴돼도 ViewModel은 살아 있고, 구독만 풀린다.
    ///
    /// 닫는 방법이 둘(X 버튼, 팝업 바깥 클릭)이지만 둘 다 ViewModel의 같은 메서드로 들어간다.
    /// 닫기 규칙이 View에 흩어지면 "어떤 경로로 닫으면 리포트가 안 지워진다" 같은 버그가 생긴다.
    ///
    /// 바깥 클릭은 전체 화면을 덮는 딤 이미지에 Button을 달아 처리한다. 딤이 팝업 패널보다
    /// 뒤에 있으므로 패널 위를 눌렀을 때는 딤의 클릭이 발생하지 않는다.
    /// </summary>
    public sealed class RewardPopupView : InjectableViewBase<LobbyViewModel>
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

        protected override void Bind(LobbyViewModel viewModel)
        {
            _rowTemplate.gameObject.SetActive(false);

            Subscribe(viewModel.IsRewardPopupOpen, open => _container.SetActive(open));
            Subscribe(viewModel.Reports, OnReportsChanged);

            // 리스너도 구독과 같은 수명에 묶어 둔다. 재주입 시 겹쳐 쌓이지 않는다.
            _closeButton.onClick.AddListener(viewModel.CloseRewardPopup);
            _dimmerButton.onClick.AddListener(viewModel.CloseRewardPopup);
            RegisterUnbind(() =>
            {
                _closeButton.onClick.RemoveListener(viewModel.CloseRewardPopup);
                _dimmerButton.onClick.RemoveListener(viewModel.CloseRewardPopup);
            });

            // 남아 있던 행이 있으면 정리한다 (재주입 대비).
            RegisterUnbind(ClearRows);
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

        private void ClearRows()
        {
            for (int i = _rows.Count - 1; i >= 0; i--)
                RemoveRow(i);
        }

        private void RebuildAll()
        {
            ClearRows();

            if (ViewModel == null) return;

            var reports = ViewModel.Reports;
            for (int i = 0; i < reports.Count; i++)
                InsertRow(i, reports[i]);
        }
    }
}
