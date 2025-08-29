#!/bin/bash
# 릴리스 태그 생성 및 GitHub에 푸시하는 스크립트

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "사용법: ./create-release.sh v1.0.0"
    exit 1
fi

echo "🚀 릴리스 $VERSION 생성 중..."

# 태그 생성
git tag -a $VERSION -m "Release $VERSION"

# GitHub에 태그 푸시
git push origin $VERSION

echo "✅ 릴리스 $VERSION 생성 완료!"
echo "📦 GitHub Actions에서 자동 빌드가 시작됩니다."
echo "🔗 릴리스 페이지: https://github.com/yamsengyi/SimpleSerialToApi/releases"
