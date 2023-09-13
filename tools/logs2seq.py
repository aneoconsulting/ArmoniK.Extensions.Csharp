from typing import IO
import requests
import json
import argparse
import tarfile
import logging
from pathlib import Path
import sys
import os
import boto3


logger = logging.getLogger(Path(__file__).name)
logging.basicConfig(
    level=logging.INFO
)


def is_valid_file(name: str) -> bool:
    """
    Select a specific file or folder based on name

    Args:
        name: The file or folder name to be checked

    Returns:
        bool: True if the file name ends with ".log" and match with the names
        defined as valid, False otherwise
    """
    return any(([name.endswith(".log") and log_file in name for log_file in args.valid_log_files]))


def make_post_request(url: str, data: str):
    """
    Make a POST request to the URL of the data

    Args:
        url: The URL to which the POST request will be made
        data: The data to be sent in the POST request body
    """
    logger.debug(f"send : {len(data)} bytes")
    requests.post(url, data=data)


def send_log_file(file: str, url: str):
    """
    Send log data from a file to a seq URL

    Args:
        file: The file object containing log data
        url: The URL to which log data will be sent
    """
    ctr = 0
    tosend = b""
    for line in file:
        line = line.decode("utf-8").strip()
        if line.startswith("{"):
            json_data = json.loads(line)
            if "@t" not in json_data.get("log"):
                continue
            ctr = ctr + 1
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
    """
    Extract log data from a tar archive and send to a seq server

    Args:
        url : The URL to which log data will be sent
        file_name : The name of the tar archive
    """
    with tarfile.open(file_name, "r") as file_obj:
        for file in file_obj.getnames():
            if is_valid_file(file):
                send_log_file(file_obj.extractfile(file), url)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Download ArmoniK logs in tar format from S3 bucket then send them to Seq.", formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument("bucket_name", help="S3 bucket", type=str)
    parser.add_argument("folder_name", help="Folder where extcsharp logs are located", type=str)
    parser.add_argument("run_number", help="GitHub workflow run_number", type=str)
    parser.add_argument("run_attempt", help="GitHub workflow run_attempt", type=str)
    parser.add_argument("file_name", help="file to download from the bucket", type=str)
    parser.add_argument("--url", dest="url", help="Seq url", type=str, default="http://localhost:9341/api/events/raw?clef")
    parser.add_argument("--file", dest="valid_log_files", nargs='+', help="List of choosen files", default=['control-plane','compute-plane'])
    args = parser.parse_args()
    

    dir_name = args.folder_name + "/" + \
        args.run_number + "/" + args.run_attempt + "/"
    tmp_dir = "./tmp/"
    obj_name = dir_name + args.file_name
    file_name = tmp_dir + obj_name

    os.makedirs(tmp_dir + dir_name, exist_ok=True)

    s3 = boto3.client('s3')
    s3.download_file(args.bucket_name, obj_name, file_name)

    extract_jsontar_log(args.url, file_name)