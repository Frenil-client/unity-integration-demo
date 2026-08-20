# Unity Integration Demo

독립적으로 만든 Unity 패키지 세 개가 하나의 화면에서 맞물리는 것을 보여주는 데모입니다.
소재는 모바일 RPG 로비입니다.

| 패키지 | 이 데모에서 맡은 역할 |
|---|---|
| [unity-stat-system](https://github.com/Frenil-client/unity-stat-system) | 캐릭터 능력치 보관 · 상한 클램프 · 변경 통지 |
| [unity-mvvm](https://github.com/Frenil-client/unity-mvvm) | 능력치/목록을 UI에 바인딩, 구독 수명 관리 |
| [unity-reddot-system](https://github.com/Frenil-client/unity-reddot-system) | 알림 집계와 배지 표시 |

소재를 RPG 로비로 잡은 이유가 있습니다. 세 패키지는 원래 모바일 RPG를 전제로 만들어졌고,
`StatId`는 공격력·마력·방어력을, `RedDotType`은 상점·캐릭터·퀘스트 메뉴 트리를 그대로 정의합니다.
그래서 이 데모는 **패키지가 정의한 용어를 번역 없이 쓰는** 예제가 됩니다.

---

## 핵심: 패키지끼리는 서로를 모른다

이 데모에서 가장 중요한 건 화면이 아니라 **결합이 어디에 있는가**입니다.

```
  Hero ──has──> Stat                          (stat-system)
                 │
                 │ event Changed(StatId, StatValue)   ← 순수 C# 이벤트
                 ▼
       StatObservableBridge                   ★ 데모의 Glue/
                 │
                 │ Observable<int>
                 ▼
       HeroCardViewModel ──> ObservableList<T>        (mvvm)
                 │                  │
                 │                  ▼
                 │            LobbyView ──> 카드 슬롯만 갱신
                 ▼
       LobbyRedDotBridge                      ★ 데모의 Glue/
                 │
                 │ SetCount(node, n)
                 ▼
           RedDotNode 트리 ──> 부모로 합계 집계 ──> 헤더 배지  (reddot-system)
```

`stat-system`은 `Observable`을 모르고, `mvvm`은 `Stat`을 모르며, `reddot-system`은 둘 다 모릅니다.
정확히 말하면 **한 패키지의 출력을 다른 패키지의 입력 타입으로 바꾸는 코드**가
[`Assets/Scripts/Glue/`](Assets/Scripts/Glue)의 두 파일뿐입니다.

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
3. `Assets/Scenes/LobbyDemo.unity` 를 열고 재생합니다

씬과 프리팹은 저장소에 들어 있으므로 별도 준비 없이 바로 돌아갑니다.

### 화면은 에디터 도구가 만든 것입니다

`Assets/Scenes/LobbyDemo.unity`와 `Assets/Prefabs/HeroCard.prefab`은 손으로 배치한 것이 아니라
[`DemoSceneBuilder`](Assets/Editor/DemoSceneBuilder.cs)가 만든 결과물입니다.

```
Tools ▸ Lobby Demo ▸ 씬과 프리팹 생성
```

실행에는 필요 없고, **화면 구조를 바꾸고 싶을 때** 쓰는 도구입니다. 캔버스 계층, 레이아웃 비율,
레드닷 배지 배치, `LobbyView`와 `LobbyDemoBootstrap`의 참조 연결까지 전부 코드로 기술되어 있어
구조 변경이 diff로 남습니다. 실행하면 기존 씬과 프리팹을 덮어씁니다.

**UI 조립 코드는 런타임에 없습니다.** 예전에는 재생할 때마다 코드로 화면을 만들었는데, 그러면
레이아웃을 바꿀 때마다 코드를 고쳐야 하고 Inspector에서 확인할 수도 없습니다. 조립을 에디터
시점으로 옮겨 프리팹과 씬으로 굳히고, 런타임에는 프리팹을 찍어 쓰기만 합니다. 사소한 조정은
생성된 프리팹을 직접 편집하는 편이 빠릅니다.

이 덕분에 레드닷 아이콘도 패키지가 의도한 방식대로 쓰입니다 — `RedDotCountIcon`의 노드 타입을
**Inspector 드롭다운에서 선택**하며, 이를 위한 별도 파생 클래스가 필요 없습니다.

### 화면 비율

`CanvasScaler`는 **너비 기준(match 0)** 으로 맞춥니다. 가로 폭이 항상 1080 단위로 고정되어
버튼과 카드 크기가 해상도와 무관하게 일정하고, 세로만 화면 비율만큼 늘거나 줄어듭니다.

그래서 세로 배치는 픽셀이 아니라 **비율로** 나눕니다.

```
┌──────────────────────────────┐
│ LOBBY                   ● 7  │  위 30%  TopSection
│ [강화][소환●4][보상●2][임무●1] │          헤더 · 버튼 · 로그
│ 로그 메시지                   │
├──────────────────────────────┤
│ 캐릭터 카드                   │  아래 70%  ListSection
│ 캐릭터 카드                   │           스크롤 목록
│ ...                          │
└──────────────────────────────┘
```

두 영역은 앵커로 잘라서 어떤 해상도에서도 30 : 70이 유지되고, 위 영역 안의 세 줄은
가중치로 남는 높이를 나눠 갖습니다. 고정 픽셀 높이를 주면 30%가 그보다 작아지는
종횡비에서 내용이 영역 밖으로 넘칩니다.

비율을 바꾸려면 `DemoSceneBuilder.TopSectionRatio` 상수 하나만 고치면 됩니다.

### 한글 표시

TMP 기본 폰트(LiberationSans)에는 한글 글리프가 없어 텍스트가 □로 나옵니다. OFL 라이선스 한글 폰트
(Noto Sans KR, 나눔고딕 등)를 `Assets/Fonts/`에 넣고 우클릭 ▸ **Create ▸ TextMeshPro ▸ Font Asset**으로
폰트 에셋을 만든 뒤, 인스펙터에서 **Atlas Population Mode를 `Dynamic`으로** 바꿉니다. 한글은 완성형
음절만 11,172자라 Static 아틀라스로는 감당이 안 됩니다.

만든 에셋을 **Edit ▸ Project Settings ▸ TextMesh Pro ▸ Settings**의 `Default Font Asset`에 지정하면
코드 수정 없이 모든 텍스트에 적용됩니다.

---

## 화면에서 확인할 수 있는 것

**강화** — 무작위 캐릭터의 능력치가 오릅니다. `Stat`이 값을 바꾸고 → 브리지가 `Observable`로 옮기고
→ 해당 카드의 숫자만 갱신됩니다. 동시에 강화 리포트가 쌓여 `보상` 버튼의 점이 올라가고,
일정 횟수마다 일일 임무가 완료되어 `임무` 버튼의 점도 올라갑니다.

능력치가 상한(999)에 도달하면 `Stat`이 클램프하고, **값이 바뀌지 않았으므로 통지도 발행되지 않습니다.**
UI는 아무 일도 하지 않고 로그만 "이미 최대치"로 바뀝니다. 변경이 없을 때 이벤트를 쏘지 않는
정책이 UI 갱신을 자동으로 줄여 주는 지점입니다.

**소환** — 목록에 카드가 하나 추가되고 버튼의 점이 하나 줄어듭니다.
`ObservableList`가 `Added` 델타 한 건만 발행하므로 기존 카드들은 다시 만들어지지 않고
새 슬롯만 생깁니다. 목록이 화면보다 길어지면 스크롤됩니다.

**보상** — 쌓인 강화 리포트를 팝업으로 보여줍니다. **여는 순간이 곧 확인**이라 그 버튼의 점이
바로 꺼집니다. 팝업은 오른쪽 위 `X` 또는 **바깥 어두운 영역을 클릭**하면 닫히고, 닫을 때
리포트 목록이 비워집니다. 닫는 방법이 둘이지만 ViewModel의 같은 메서드로 들어가므로
"어떤 경로로 닫으면 목록이 안 지워진다" 같은 어긋남이 생기지 않습니다.

**임무** — 완료된 일일 임무 보상을 수령하고 그 버튼의 점이 꺼집니다.

### 레드닷이 어디에 붙어 있나

알림 세 종류가 **각각 다른 가지**에 들어가고, 합계는 트리가 만듭니다.

```
MainMenu ● 7          ← 헤더 배지 (합계)
├─ Shop      ● 4      └ ShopPackage       남은 소환권      → 소환 버튼
├─ Character ● 2      └ CharacterLevelUp  미확인 강화 리포트 → 보상 버튼
└─ Quest     ● 1      └ QuestDaily        완료된 일일 임무   → 임무 버튼
```

각 알림은 **그것을 처리하는 버튼**에 점으로 붙습니다. 강화 버튼에는 점이 없는데, 알림을
만드는 쪽이지 처리하는 쪽이 아니기 때문입니다.

헤더 배지는 세 가지의 최상위 부모라 합계가 자동으로 올라옵니다. 배지 쪽에는 합산 코드가
한 줄도 없고 `RedDotNode`가 델타로 굴려 올릴 뿐입니다. 한 버튼의 점을 꺼도 헤더 숫자가
남아 있다면 다른 가지에 처리할 것이 남아 있다는 뜻이고, 이는 트리가 실제로 합산하고 있다는
증거이기도 합니다.

`Window ▸ RedDot ▸ Tree Debugger`를 열어 두면 재생 중에 어느 노드에 값이 들어가고
어떻게 부모로 올라가는지 실시간으로 보입니다.

---

## 테스트

`Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All` — 16종.

Canvas도 GameObject도 만들지 않고 흐름 전체를 검증합니다. 도메인과 ViewModel이 Unity에
의존하지 않기 때문인데, unity-mvvm이 ViewModel을 MonoBehaviour로 만들지 않은 이유가 정확히 이것입니다.

고정한 것: 소환이 `Added` 델타 한 건만 내는지, 능력치 변경이 브리지를 거쳐 올바른 값으로
도착하는지, 변화 없는 강화가 침묵하는지, 상한 클램프가 걸리는지, 세 알림 소스가 서로
독립적으로 오르내리는지, `Dispose`가 `Stat.Changed` 구독을 실제로 푸는지.

---

## 이 데모가 드러낸 것

패키지만 만들고 끝냈으면 못 찾았을 문제들이 통합 과정에서 나왔습니다. 데모의 목적 중 하나가
이런 걸 찾는 것이라 감추지 않고 적어 둡니다.

**1. `.meta` 파일 누락** (해결) — stat/reddot 패키지에 `.meta`가 하나도 없어, git URL로 설치하면
Unity가 immutable 폴더에 meta를 만들지 못해 **에셋 전체가 무시**됐습니다. asmdef도 임포트되지 않아
어셈블리 자체가 생기지 않았죠. 로컬 `Assets/` 복사로는 재현되지 않고 **UPM 설치에서만** 터지는
종류의 버그입니다.

**2. 초기화 순서 의존** (해결) — `RedDotIcon.OnEnable`이 `RedDotManager.Awake`보다 먼저 돌면
아이콘이 어떤 노드에도 연결되지 못한 채 죽었습니다. 트리를 첫 접근 시 스스로 구성하는
`RedDotTree`로 옮겨 순서라는 개념 자체를 없앴습니다.

**3. `ViewBase<T>`가 ViewModel 주입을 지원하지 않는다** (미해결) — 제약이
`where TViewModel : ViewModelBase, new()`라, 생성자 인자가 필요한 ViewModel은 타입 인자로
넣는 것조차 불가능합니다. 그래서 [`HeroCardView`](Assets/Scripts/UI/HeroCardView.cs)는
`ViewBase`를 쓰지 못하고 `Bind`/`Unbind`를 직접 관리합니다. unity-mvvm에서 주입을 1급으로
다루도록 고칠 지점입니다.

---

## 구조

```
Assets/Scripts/
├─ Domain/            캐릭터·보유 목록·강화 (순수 C#, StatSystem만 사용)
│  ├─ Hero.cs
│  └─ HeroRoster.cs
├─ Glue/              ★ 패키지 간 결합이 존재하는 유일한 곳
│  ├─ StatObservableBridge.cs   Stat.Changed -> Observable<int>
│  └─ LobbyRedDotBridge.cs      Observable<int> -> RedDot 노드 카운트
├─ UI/
│  ├─ LobbyViewModel.cs         목록·로그·알림 수 (Unity 비의존)
│  ├─ HeroCardViewModel.cs      카드 하나의 파생 상태 (Unity 비의존)
│  ├─ LobbyView.cs              ViewBase 상속, ListChange 델타 처리
│  ├─ HeroCardView.cs           카드 프리팹의 표시 담당
│  └─ RewardPopupView.cs        강화 리포트 팝업 (X · 바깥 클릭으로 닫기)
└─ Bootstrap/
   └─ LobbyDemoBootstrap.cs     ViewModel과 레드닷 트리를 잇는 브리지 생성

Assets/Editor/
└─ DemoSceneBuilder.cs           씬·프리팹 생성 도구 (런타임에 포함되지 않음)

Assets/Prefabs/  Assets/Scenes/  ← 위 도구가 생성한 결과물 (저장소에 포함)
```

## 요구 사항

- Unity 6 (6000.3.9f1) — `ProjectSettings/ProjectVersion.txt` 기준. 패키지 자체는 Unity 2021.3 이상에서 동작합니다
- TextMeshPro — Unity 6에서는 `com.unity.ugui`에 포함되어 있습니다
