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


def is_valid_file(name: str) -> bool:
    if name.find("control") != -1 and name.endswith(".log"):
        return True
    return False


def filter_logs(file_list: list[str]) -> list[str]:
    result = []
    # file:str
    for file in file_list:
        if is_valid_file(file):
            result.append(file)
    return result


def make_post_request(url, data):
    try:
        resp = requests.post(url, data=data)
        resp.raise_for_status()

        return resp.content.decode("utf-8")

    except requests.exceptions.RequestException as e:
        return f"Une erreur s'est produite lors de la requête : {e}"


def send_log_file(name: str, file_obj: tarfile.TarFile, url: str):
    batch = 0
    ctr = 0
    tosend = ""
    file = file_obj.extractfile(name)
    for line in file:
        line = line.decode("utf-8").strip()
        if line.startswith("{"):
            try:
                json_data = json.loads(line)
                # if "log" not in json_data:
                #     continue
                log_message = json_data.get("log")
                tosend += log_message
                logging.debug(log_message)
            except json.JSONDecodeError:
                pass
        if batch > 1000:
            make_post_request(url, tosend)
            tosend = ""
            batch = 0
        batch = batch + 1
        ctr = ctr + 1
    logging.info(f"sent : {ctr}",)
    if tosend != "":
        make_post_request(url, tosend)


def extract_jsontar_log(url: str, file_name: str):
    file_obj = tarfile.open(file_name, "r")
    file_list = file_obj.getnames()
    file_list = filter_logs(file_list)
    # logging.debug(file_list)
    for file in file_list:
        send_log_file(file, file_obj, url)

    file_obj.close()


if __name__ == "__main__":

    # Création du logger
    logger = logging.getLogger(__file__)
    logging.basicConfig(
        # Définit le niveau à partir du quel les logs sont affichés
        level=logging.INFO
    )

    parser = argparse.ArgumentParser(description=" Local ArmoniK logs in JSON CLEF format then send them to Seq.",
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)

    parser.add_argument("--url", dest="url", help="Seq url", type=str,
                        default="http://localhost:9341/api/events/raw?clef")
    args = parser.parse_args()

    filename = "/home/jeremyzynger/test_logs/end2end-false-false-disable-false.tar"

    if not Path(filename).exists():
        logger.critical(f"ERROR : file does not exist {filename}")

    extract_jsontar_log(args.url, filename)
