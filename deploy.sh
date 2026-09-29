#!/bin/bash
# Builds the BookingApi image, pushes it to ACR, and tags the commit it was built from.
# Mirrors FkBorovany/deploy.sh: same registry, same YYYYMMDD.N version scheme, so both
# halves of the deployment are driven the same way. Each repo deploys itself - the
# dirty-tree check and `git tag` only mean anything inside the repo being built, which
# is why this lives here and not in deploy/ (which is not a git repo).
#
# Authenticate first: an active `az login` session for the subscription owning jpreg.
#
# Usage:
#   ./deploy.sh              # auto version: today's date + next free .N suffix
#   ./deploy.sh 20260903.4   # explicit version
#
# After this succeeds, the image is in ACR but nothing is deployed yet:
#   1. set the `api` image tag in deploy/docker-compose.production.yml to the tag printed below
#   2. on the server: docker compose -f docker-compose.production.yml pull api
#                     docker compose -f docker-compose.production.yml up -d api
# The container applies EF migrations on startup (Program.cs -> MigrateDatabaseAsync),
# so rolling back is not just re-pulling the previous tag once a migration has run.

set -euo pipefail

REGISTRY="jpreg.azurecr.io"
IMAGE="booking-api"
ACR_NAME="jpreg"
# Pinned: the server is x86_64, and an ARM build would push a silently unrunnable image.
PLATFORM="linux/amd64"

cd "$(dirname "$0")"

# Refuse to tag a commit that doesn't match what actually gets built.
if [ -n "$(git status --porcelain)" ]; then
  echo "Error: working tree has uncommitted changes. Commit or stash before deploying." >&2
  exit 1
fi

if [ -n "${1:-}" ]; then
  VERSION="$1"
else
  today=$(date +%Y%m%d)
  last=$(git tag -l "${today}.*" | sed "s/^${today}\.//" | sort -n | tail -1)
  VERSION="${today}.$((${last:-0} + 1))"
fi

# Checked up front, not left to `git tag` at the end: by then the image would already
# be pushed, overwriting an existing ACR tag while the commit tag fails to apply.
if git rev-parse -q --verify "refs/tags/${VERSION}" > /dev/null; then
  echo "Error: tag ${VERSION} already exists. Pick another version." >&2
  exit 1
fi

TAG="${REGISTRY}/${IMAGE}:${VERSION}"

az account show > /dev/null 2>&1 || {
  echo "Error: not logged in to Azure. Run 'az login' first." >&2
  exit 1
}

echo "==> az acr login -n $ACR_NAME"
az acr login -n "$ACR_NAME"

# Context is src/, not the repo root: src/Dockerfile's COPY paths ("BookingApi/
# BookingApi.csproj" and friends) are relative to it.
echo "==> docker build -t $TAG"
docker build --platform "$PLATFORM" -t "$TAG" -f src/Dockerfile src

echo "==> docker push $TAG"
docker push "$TAG"

echo "==> git tag $VERSION"
git tag "$VERSION"
git push origin "$VERSION"

echo
echo "Done. Pushed $TAG and tagged commit $(git rev-parse --short HEAD) as $VERSION."
echo "Next, in deploy/docker-compose.production.yml:"
echo "    image: $TAG"
