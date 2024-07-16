import requests
import time
from requests.packages.urllib3.exceptions import InsecureRequestWarning
import math
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)



def register():
    return requests.post(url = "https://localhost:7272/Powergrid/Register",json={"name":"test","type":"Powerplant"}, verify=False).text

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



key = register()
energy_pro_call = calc_producing(key=key)
print(energy_pro_call)
HOURS_24 = needed_energy()
current_energy = energy_pro_call
current_time = get_time()
current_tick = 0
recalc_time = 256

def tick(energy_pro_call: int,key: str,needed: float,current_energy: float):
    if current_energy < needed:
        print(f"Needed {needed}")
        calls = math.ceil((needed - current_energy ) / energy_pro_call)
        print(f"Calls needed: {calls}")
        for i in range(calls):
            power_up(key)
            current_energy += energy_pro_call  
            print(i)
        current_energy -= needed
        print(f"Current energy: {current_energy}")
    



while True != 0:
    tick(energy_pro_call=current_energy,key=key,needed=float(HOURS_24[str(current_time)]),current_energy=current_energy)
    current_tick += 1
    print(f"Time: {current_time}")
    if (current_time + 1) % 24 == 0:
        HOURS_24 = needed_energy()
    
    if current_tick % recalc_time == 0:
        current_time = get_time()
    current_time += 1
    time.sleep(12)
    