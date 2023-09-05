from typing import IO
import requests
import json
import argparse
import tarfile
import logging
from pathlib import Path
import sys
import os


# How to run seq in docker
# docker rm -f seqlogpipe
# docker run -d --rm --name seqlogpipe -e ACCEPT_EULA=Y -p 9080:80 -p 9341:5341 datalust/seq

# Création du logger
logger = logging.getLogger(Path(__file__).name)
logging.basicConfig(
    # Définit le niveau à partir du quel les logs sont affichés
    level=logging.INFO
)

VALID_LOG_FILES = [
    'control',
    'compute'
]


def is_valid_file(name: str) -> bool:
    if any([name.find(log_file) != -1 for log_file in VALID_LOG_FILES]) and name.endswith(".log"):
        return True
    return False


def make_post_request(url: str, data: str):
    logger.debug(f"send : {len(data)} bytes")
    requests.post(url, data=data)
    # logger.info(resp.json())
    # # resp.raise_for_status()
    # return resp.content.decode("utf-8"), False


def send_log_file(file: IO[bytes], url: str):
    ctr = 0
    tosend = b""
    for line in file:
        line = line.decode("utf-8").strip()
        if line.startswith("{"):
            ctr = ctr + 1
            json_data = json.loads(line)
            log_message = bytes(json_data.get("log"), "utf-8")

            if len(tosend) + len(log_message) > 100000:
                make_post_request(url, tosend)
                tosend = log_message
            else:
                tosend += log_message

    logger.info(f"sent : {ctr}")
    if tosend != b"":
        make_post_request(url, tosend)


def extract_jsontar_log(url: str, file_name: str):
    with tarfile.open(file_name, "r") as file_obj:
        for file in file_obj.getnames():
            if is_valid_file(file):
                send_log_file(file_obj.extractfile(file), url)


if __name__ == "__main__":

    parser = argparse.ArgumentParser(description=" Local ArmoniK logs in JSON CLEF format then send them to Seq.",
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)

    parser.add_argument("--url", dest="url", help="Seq url", type=str,
                        default="http://localhost:9341/api/events/raw?clef")
    args = parser.parse_args()

    # filename = "/home/jeremyzynger/test_logs/end2end-false-false-disable-false.tar"
    filename = "/home/jeremyzynger/armonik/ArmoniK.Core/test.tar.gz"
    if not Path(filename).exists():
        logger.critical(f"ERROR : file does not exist {filename}")

    extract_jsontar_log(args.url, filename)
