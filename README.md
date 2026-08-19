# Unity Integration Demo

독립적으로 만든 Unity 패키지 세 개가 하나의 화면에서 맞물리는 것을 보여주는 데모입니다. 소재는 축구 스쿼드 관리입니다.

| 패키지 | 이 데모에서 맡은 역할 |
|---|---|
| [unity-stat-system](https://github.com/Frenil-client/unity-stat-system) | 선수 능력치 보관 · 상한 클램프 · 변경 통지 |
| [unity-mvvm](https://github.com/Frenil-client/unity-mvvm) | 능력치/목록을 UI에 바인딩, 구독 수명 관리 |
| [unity-reddot-system](https://github.com/Frenil-client/unity-reddot-system) | 미확인 알림 집계와 헤더 배지 표시 |

---

## 핵심: 패키지끼리는 서로를 모른다

이 데모에서 가장 중요한 건 화면이 아니라 **결합이 어디에 있는가**입니다.

```
  SquadPlayer ──has──> Stat                    (stat-system)
                        │
                        │ event Changed(StatId, StatValue)   ← 순수 C# 이벤트
                        ▼
              StatObservableBridge             ★ 데모의 Glue/
                        │
                        │ Observable<int>
                        ▼
              PlayerCardViewModel ──> ObservableList<T>   (mvvm)
                        │                    │
                        │                    ▼
                        │              SquadView ──> 카드 슬롯만 갱신
                        ▼
              SquadRedDotBridge               ★ 데모의 Glue/
                        │
                        │ SetCount(node, n)
                        ▼
                  RedDotNode 트리 ──> 부모로 합계 집계 ──> 헤더 배지  (reddot-system)
```

`stat-system`은 `Observable`을 모르고, `mvvm`은 `Stat`을 모르며, `reddot-system`은 둘 다 모릅니다.
세 패키지를 잇는 코드는 전부 이 저장소의 [`Assets/Scripts/Glue/`](Assets/Scripts/Glue) 안에만 있습니다.

이게 왜 중요하냐면, 각 패키지의 README가 "외부 의존 없는 드롭인"이라고 주장하기 때문입니다.
연결 코드를 패키지 안에 넣는 순간 그 주장이 깨집니다. 그래서 `Stat`은 UI 프레임워크를 모르는
순수 C# 이벤트만 발행하고, 그것을 `Observable<int>`로 옮기는 어댑터는 두 패키지를 **함께 쓰는 쪽**,
즉 이 데모에만 존재합니다.

---

## 왜 저장소를 합치지 않았나

세 패키지는 각각 `package.json`을 가진 독립 UPM 패키지입니다. 하나의 저장소로 합치면
"재사용 가능한 라이브러리"라는 성격이 "예제 프로젝트 하나"로 내려앉습니다.

기술적인 이유도 있습니다. Unity는 **패키지의 `dependencies` 필드에 git URL을 넣지 못합니다**
(레지스트리 버전만 허용). git URL을 쓸 수 있는 곳은 프로젝트의 `Packages/manifest.json`뿐이라,
"세 패키지를 의존하는 데모 패키지"는 애초에 만들 수 없고 데모는 **프로젝트**여야 합니다.

덤으로, 이 저장소가 git URL로 세 패키지를 당겨오는 것 자체가 각 README에 적힌 설치 경로가
실제로 동작한다는 증명이 됩니다.

```json
{
  "dependencies": {
    "com.frenil.mvvm": "https://github.com/Frenil-client/unity-mvvm.git#v1.1.1",
    "com.frenil.stat-system": "https://github.com/Frenil-client/unity-stat-system.git#v2.0.1",
    "com.frenil.reddot-system": "https://github.com/Frenil-client/unity-reddot-system.git#v1.1.2"
  }
}
```

버전은 태그로 고정했습니다. 브랜치로 당겨오면 패키지를 고칠 때마다 데모가 조용히 깨집니다.

---

## 실행 방법

1. Unity 6 (6000.3.9f1)로 이 프로젝트를 엽니다 (패키지는 manifest에서 자동으로 받아옵니다)
2. TextMeshPro Essentials 임포트 창이 뜨면 **Import**를 누릅니다
3. 한글 폰트를 준비합니다 — 아래 "한글 표시" 참고
4. 메뉴에서 **Tools ▸ Squad Demo ▸ 씬과 프리팹 생성**을 실행합니다
5. 만들어진 `Assets/Scenes/SquadDemo.unity`를 열고 재생합니다

4번이 만드는 것은 두 가지입니다.

- `Assets/Prefabs/PlayerCard.prefab` — 선수 카드 프리팹. `SquadView`가 목록 항목마다 하나씩 찍습니다
- `Assets/Scenes/SquadDemo.unity` — 캔버스·버튼·레드닷 배지·`RedDotManager`가 배치되고
  `SquadView`와 `SquadDemoBootstrap`의 참조가 전부 연결된 씬

**UI 조립 코드는 런타임에 없습니다.** 예전에는 재생할 때마다 코드로 화면을 만들었는데, 그러면
레이아웃을 바꿀 때마다 코드를 고쳐야 하고 Inspector에서 확인할 수도 없습니다. 조립은
[`DemoSceneBuilder`](Assets/Editor/DemoSceneBuilder.cs)가 에디터에서 한 번만 수행해 프리팹과 씬으로
굳히고, 런타임에는 프리팹을 찍어 쓰기만 합니다. 레이아웃을 손보고 싶으면 생성된 프리팹을 직접
편집하는 편이 빠릅니다.

이 덕분에 레드닷 아이콘도 패키지가 의도한 방식대로 쓰입니다 — `RedDotCountIcon`의 노드 타입을
**Inspector 드롭다운에서 선택**하며, 이를 위한 별도 파생 클래스가 필요 없습니다.

### 한글 표시

TMP 기본 폰트(LiberationSans)에는 한글 글리프가 없어 텍스트가 □로 나옵니다. OFL 라이선스 한글 폰트
(Noto Sans KR, Pretendard 등)를 `Assets/Fonts/`에 넣고 우클릭 ▸ **Create ▸ TextMeshPro ▸ Font Asset**으로
폰트 에셋을 만든 뒤, 인스펙터에서 **Atlas Population Mode를 `Dynamic`으로** 바꿉니다. 한글은 완성형
음절만 11,172자라 Static 아틀라스로는 감당이 안 됩니다.

만든 에셋을 **Edit ▸ Project Settings ▸ TextMesh Pro ▸ Settings**의 `Default Font Asset`에 지정하면
코드 수정 없이 모든 텍스트에 적용됩니다.

---

## 화면에서 확인할 수 있는 것

**훈련** — 무작위 선수의 능력치가 오릅니다. `Stat`이 값을 바꾸고 → 브리지가 `Observable`로 옮기고
→ 해당 카드의 숫자만 갱신됩니다. 동시에 미확인 리포트 수가 늘어 헤더 배지의 숫자가 올라갑니다.

능력치가 99에 도달하면 `Stat`이 클램프하고, **값이 바뀌지 않았으므로 통지도 발행되지 않습니다.**
UI는 아무 일도 하지 않고 로그만 "이미 최대치"로 바뀝니다. 변경이 없을 때 이벤트를 쏘지 않는
정책이 UI 갱신을 자동으로 줄여 주는 지점입니다.

**영입** — 목록에 카드가 하나 추가되고 버튼의 빨간 점이 하나 줄어듭니다.
`ObservableList`가 `Added` 델타 한 건만 발행하므로 기존 카드들은 다시 만들어지지 않고
새 슬롯만 생깁니다. 목록이 화면보다 길어지면 스크롤됩니다.
유망주를 다 영입하면 점이 사라지고 버튼이 비활성화됩니다.

**리포트 확인** — 훈련 리포트 수가 0이 되어 그 버튼의 점이 꺼집니다.

### 레드닷이 어디에 붙어 있나

알림은 두 종류이고, 각각 **그 알림을 처리하는 버튼**에 점으로 표시됩니다.

| 위치 | 노드 | 의미 |
|---|---|---|
| `영입` 버튼 | `CharacterEquipment` | 아직 영입하지 않은 유망주 수 |
| `리포트 확인` 버튼 | `CharacterLevelUp` | 확인하지 않은 훈련 리포트 수 |
| 헤더 배지 | `Character` | **위 둘의 합계** |

헤더 배지는 두 노드의 부모라서 합계가 자동으로 올라옵니다. 배지 쪽에는 합산 코드가 한 줄도 없고
`RedDotNode`가 델타로 굴려 올릴 뿐입니다. 리포트를 다 확인해도 헤더 숫자가 남아 있다면
영입할 유망주가 남아 있다는 뜻이고, 이는 트리가 실제로 합산하고 있다는 증거이기도 합니다.

`Window ▸ RedDot ▸ Tree Debugger`를 열어 두면 재생 중에 어느 노드에 값이 들어가고
어떻게 부모로 올라가는지 실시간으로 보입니다.

---

## 테스트

`Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All` — 10종.

Canvas도 GameObject도 만들지 않고 흐름 전체를 검증합니다. 도메인과 ViewModel이 Unity에
의존하지 않기 때문인데, unity-mvvm이 ViewModel을 MonoBehaviour로 만들지 않은 이유가 정확히 이것입니다.

고정한 것: 영입이 `Added` 델타 한 건만 내는지, 스탯 변경이 브리지를 거쳐 올바른 값으로
도착하는지, 변화 없는 훈련이 침묵하는지, 상한 클램프가 걸리는지, `Dispose`가 `Stat.Changed`
구독을 실제로 푸는지.

---

## 이 데모가 드러낸 것

데모를 만드는 과정에서 패키지 쪽 설계 문제 두 가지가 나왔습니다. 데모의 목적 중 하나가
이런 걸 찾는 것이라, 감추지 않고 적어 둡니다.

**1. `ViewBase<T>`가 ViewModel 주입을 지원하지 않는다.**
제약이 `where TViewModel : ViewModelBase, new()`라, 생성자 인자가 필요한 ViewModel은
타입 인자로 넣는 것조차 불가능합니다. 목록의 각 항목처럼 "바깥에서 만들어 주입받는"
ViewModel이 프레임워크의 기본 경로에서 빠져 있는 셈입니다. 그래서
[`PlayerCardView`](Assets/Scripts/UI/PlayerCardView.cs)는 `ViewBase`를 쓰지 못하고
`Bind`/`Unbind`를 직접 관리합니다. unity-mvvm에서 주입을 1급으로 다루도록 고칠 지점입니다.

**2. `RedDotType`이 enum이라 데모가 노드를 추가할 수 없다.**
패키지가 제공하는 enum이므로 기존 `Character` 계열 노드에 데모의 의미를 얹었습니다.
타입 안전하고 Inspector에서 고르기 좋다는 장점의 뒷면으로, 새 콘텐츠의 레드닷을 추가하려면
클라이언트를 다시 빌드해야 한다는 뜻이기도 합니다. 라이브 서비스에서는 데이터 주도 키와의
하이브리드를 고려할 지점입니다.

---

## 구조

```
Assets/Scripts/
├─ Domain/            선수·스쿼드·훈련 (순수 C#, StatSystem만 사용)
│  ├─ SquadPlayer.cs
│  └─ SquadRoster.cs
├─ Glue/              ★ 패키지 간 결합이 존재하는 유일한 곳
│  ├─ StatObservableBridge.cs   Stat.Changed -> Observable<int>
│  └─ SquadRedDotBridge.cs      Observable<int> -> RedDot 노드 카운트
├─ UI/
│  ├─ SquadViewModel.cs         목록·로그·미확인 수 (Unity 비의존)
│  ├─ PlayerCardViewModel.cs    카드 하나의 파생 상태 (Unity 비의존)
│  ├─ SquadView.cs              ViewBase 상속, ListChange 델타 처리
│  └─ PlayerCardView.cs         카드 프리팹의 표시 담당
└─ Bootstrap/
   └─ SquadDemoBootstrap.cs     ViewModel과 레드닷 트리를 잇는 브리지 생성

Assets/Editor/
└─ DemoSceneBuilder.cs           씬·프리팹 생성 도구 (런타임에 포함되지 않음)

Assets/Prefabs/  Assets/Scenes/  ← 위 도구가 생성
```

## 요구 사항

- Unity 6 (6000.3.9f1) — `ProjectSettings/ProjectVersion.txt` 기준. 패키지 자체는 Unity 2021.3 이상에서 동작합니다
- TextMeshPro — Unity 6에서는 `com.unity.ugui`에 포함되어 있습니다
