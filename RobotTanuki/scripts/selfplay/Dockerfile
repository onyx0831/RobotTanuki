# 自己対局の実行環境。.NET SDKでエンジンをビルドし、python-shogiで対局を管理する。
FROM mcr.microsoft.com/dotnet/sdk:9.0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-venv \
    && rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir python-shogi==1.1.1
ENV PATH=/opt/venv/bin:$PATH

# マウントしたリポジトリはコンテナのユーザーと所有者が異なり、gitの所有者チェックで弾かれるため。
RUN git config --system --add safe.directory '*'

# スクリプトもイメージに含め、ホストでどのブランチをチェックアウトしていても実行できるようにする。
# スクリプトを変更したらイメージを作り直す。
COPY run.sh selfplay.py /opt/selfplay/
ENTRYPOINT ["/opt/selfplay/run.sh"]
