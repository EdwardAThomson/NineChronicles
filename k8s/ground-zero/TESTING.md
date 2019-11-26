이 문서에서는 정기 테스트 당번이 정기 테스트를 진행하기 위해 준비해야 하는 것들에 대해서 다룹니다.

# 준비하기

## Azure CLI

1. Swen 에게 요청해서 Azure 포탈 계정을 생성합니다.
2. [공식 문서](https://docs.microsoft.com/ko-kr/cli/azure/install-azure-cli?view=azure-cli-latest)를 참조하여 Azure CLI 를 설치합니다.

## AWS CLI

1. Swen 에게 요청해서 AWS IAM 계정을 생성합니다.
2. [공식 문서](https://docs.aws.amazon.com/ko_kr/cli/latest/userguide/cli-chap-install.html)를 참조하여 AWS CLI를 설치합니다.

# 시작 버전 정하기

- 테스트 기간에 사용할 9C 빌드 버전을 정하기 위해서, 에디터에서 검수된 태그 (예. [1](https://github.com/planetarium/nekoyume-unity/tree/20190910-01), [2](https://github.com/planetarium/nekoyume-unity/tree/20190906-01))를 확인합니다.
- Windows x64, macOS, Linux (헤들리스)에서 모두 정상 동작하는 버전 중, 가장 최신 태그를 기준으로 결정합니다.
- [9C 업스트림](https://github.com/planetarium/nekoyume-unity)의 `master`브랜치에서 관리 되고 있는 9C 코드를 쓰는 것을 기본으로 하되, 부득이 이를 사용할 수 없는 경우 별도 브랜치를 따로 따서 팀원이 접근할 수 있는 저장소에 푸시한 후, 테스트 진행 문서에 추가합니다.

# 시드 노드 빌드하기

네트워크를 운영하기 위해서는 시드(Seed) 노드가 필요합니다. 정기 테스트에서는 이 시드 노드를 Docker 이미지로 빌드하여 Kubernetes(k8s) 를 통해 실행합니다.

시드 노드 이미지를 만들기 위해서는 CircleCI를 사용하거나 프로그래머의 개발 환경에서 직접 빌드할 수 있습니다.

## CircleCI 사용

9C 업스트림에 풀 리퀘스트를 올리면 자동으로 CircleCI가 붙어서 Docker 이미지를 빌드하고 지정한 레지스트리에 푸시합니다. 이 이미지 이름은 CircleCI 빌드 결과 페이지에서 확인할 수 있습니다.

[](https://www.notion.so/7410267a49e947799e15169f8955e87d#5c6914008a594a72a43e5dbb4241684c)

예를 들어 [https://circleci.com/gh/planetarium/nekoyume-unity/1523](https://circleci.com/gh/planetarium/nekoyume-unity/1523) 배포는 [`planetariumtest.azurecr.io/nekoyume-unity:git-4610a11c4075a226446b4ea40252753ecb5af8db`](http://planetariumtest.azurecr.io/nekoyume-unity:git-4610a11c4075a226446b4ea40252753ecb5af8db) 로 태깅되어 planetariumtest.azurecr.io 에 푸시됩니다.

## 개발 환경에서 직접 빌드 & 푸시하기

Docker가 설치된 환경이라면 다음 명령어로 이미지를 빌드할 수 있습니다.

    docker build -t planetarium.azurecr.io/nekoyume-unity:<적절한 TAG> --build-arg ulf="<Base64로 인코딩 된 ULF>" .

- ULF(Unity License File)은 다음과 같은 방법으로 만들 수 있습니다.
    - [이 링크](https://docs.unity3d.com/kr/2019.1/Manual/ManualActivationGuide.html)를 참고하여 직접 만든 다음 base64로 인코딩 하거나
        - 주의) Docker 컨테이너 안에 있는 Unity 에디터에서 요청을 생성해야 합니다.
    - 1Password Vault에서 `Swen's ULF (base64 encoded)`항목을 복붙

만들어진 이미지를 저장소에 푸시하기 위해서는 우선 [Azure Container Registry](https://azure.microsoft.com/ko-kr/services/container-registry/) 인증을 사용하고 있는 Docker 클라이언트에 통합해야 합니다.

    $ az acr login --name planetarium

그 다음 Docker 이미지를 지정한 레지스트리에 푸시합니다.

    docker push planetarium.azurecr.io/nekoyume-unity:<빌드에 사용한 TAG>

# k8s 클러스터 설정하기

- [Amazon EKS](https://aws.amazon.com/ko/eks/)에서 돌아가는 [9c-internal 클러스터](https://ap-northeast-2.console.aws.amazon.com/eks/home?region=ap-northeast-2#/clusters/9c-internal)를 사용하고 있습니다. 이 클러스터를 사용하기 위해서 아래와 같은 단계로 인증합니다.

        $ aws configure
        AWS Access Key ID [None]: <발급 받은 IAM 계정 엑세스 키>
        AWS Secret Access Key [None]: <발급 받은 IAM 계정 비밀 엑세스 키>
        Default region name [None]: ap-northeast-2
        Default output format [None]: json

        $ aws eks update-kubeconfig --name 9c-internal

- [다음 페이지](https://codepen.io/hongminhee/pen/LBJPQp)에서 시드 노드에서 사용할 비밀키 / 공개 키를 생성합니다.
- `k8s/ground-zero/deployment.yaml.template` 을 적절히 수정하여 `k8s/ground-zero/deployment.yaml`을 만듭니다.
- 다음 명령으로 변경된 템플릿을 클러스터에 반영합니다.

        $ kubectl apply -f k8s/ground-zero/deployment.yaml --role-arn arn:aws:iam::319679068466:role/EKS

# 클라이언트 빌드 하기

- `nekoyume/Assets/StreamingAssets/clo.json` 파일을 만들어서 아래와 같이 편집합니다.

        {
            "noMiner": true,
            "peers": ["<시드 노드의 공개키>,ground-zero.planetarium.dev,31234"]
        }

    - `clo.json`파일의 자세한 설정은 nekoyume-unity 프로젝트의 [README](https://github.com/planetarium/nekoyume-unity/blob/266b6ee/README.md)를 참고하세요.

- Unity (2019.1) 에디터의 상단 메뉴의 Build → Windows / Mac OSX / Linux 빌드를 눌러 프로젝트를 빌드합니다.
- `nekoyume/Build`에 플랫폼 별로 생성된 빌드 폴더를 zip으로 압축해서 공유합니다.
    - Windows에서 7z으로 압축시 macOS에서 실행 권한이 없다고 나오는 경우가 있습니다.