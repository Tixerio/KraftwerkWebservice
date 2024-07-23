import requests
import time
from requests.packages.urllib3.exceptions import InsecureRequestWarning
import math
import random
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)
KRAFTWERK_COUNT = 10


def register():
    return requests.post(url = "https://localhost:7272/Powergrid/Register",json={"name":"test" + str(random.randint(0,219301231)),"type":"Powerplant"}, verify=False).text

def power_up(key: str):
    requests.post(url="https://localhost:7272/Powergrid/ChangeEnergy",json=(key), verify=False)

def get_energy():
    return float(requests.get(url="https://localhost:7272/Powergrid/GetEnergy", verify=False).text)

def get_time():
    return int(requests.get(url="https://localhost:7272/Powergrid/GetTime",verify=False).text)

def calc_producing(key: str):
    initial = get_energy() 
    power_up(key)
    return get_energy() - initial

def needed_energy():
    return requests.get(url="https://localhost:7272/Powergrid/GetExpectedConsume", verify=False).json()



keys = [register() for i in range(KRAFTWERK_COUNT)]
energy_pro_call = calc_producing(key=keys[0])
print(energy_pro_call)
HOURS_24 = needed_energy()
current_energy = energy_pro_call
current_time = get_time()
current_tick = 0
needed = 0
def tick():
    global current_energy
    global energy_pro_call
    global current_time
    global current_tick
    global needed
    current_energy -= needed
    print(f"Needed {needed}")
    calls = math.ceil((needed - current_energy ) / energy_pro_call)
    print(f"Calls needed: {calls}")
    if current_energy > (needed + 1000):
        return
    for i in range(calls):
        power_up(keys[random.randrange(0,KRAFTWERK_COUNT-1)])
        current_energy += energy_pro_call
    print(f"Current energy: {current_energy}")



while True != 0:
    needed = float(HOURS_24[str(current_time % 24)])
    tick()
    current_tick += 1
    print(f"Time: {current_time}")
    if (current_time + 1) % 24 == 0:
        HOURS_24 = needed_energy()
    
    if current_tick % 6 == 0:
        current_energy = get_energy()
        current_time = get_time()
    time.sleep(2)
    