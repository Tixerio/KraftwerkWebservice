import requests
import time
from requests.packages.urllib3.exceptions import InsecureRequestWarning
import math
import random
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)
KRAFTWERK_COUNT = 10

with open("greek_names.txt","r") as f:
    names = f.readlines()
    random.shuffle(names)

# This function is used to register a powerplant in the system
def register(name: str):
    return requests.post(url = "https://localhost:7272/Powergrid/Register",json={"name": name, "type":"Powerplant"}, verify=False).text

# This function generates power for the powerplant with the given key
def power(key: str):
    requests.post(url="https://localhost:7272/Powergrid/ChangeEnergy",json=(key), verify=False)

# This function returns the current energy in the system
def get_energy():
    return float(requests.get(url="https://localhost:7272/Powergrid/GetEnergy", verify=False).text)

# This function returns the current time
def get_time():
    return int(requests.get(url="https://localhost:7272/Powergrid/GetTime",verify=False).text)


# This function calculates the energy produced by the powerplant with the given key, by comparing the energy before and after the power generation
def calc_producing(key: str):
    initial = get_energy() 
    power(key)
    return get_energy() - initial


# This function returns the energy needed for the next 24 hours
def needed_energy():
    return requests.get(url="https://localhost:7272/Powergrid/GetExpectedConsume", verify=False).json()


# Register the powerplants
keys = [register(names[i]) for i in range(KRAFTWERK_COUNT)]
# Calculate the energy produced by the first powerplant, we assume that all powerplants produce the same amount of energy
energy_pro_call = calc_producing(key=keys[0])
print(energy_pro_call)

# Get the energy needed for the next 24 hours
HOURS_24 = needed_energy()

# Initialize the variables
current_energy = energy_pro_call

# Get the current time
current_time = get_time()

# Initialize the tick counter
current_tick = 0

# Initialize the energy needed
needed = 0
def tick():
    # Declare the global variables
    global current_energy
    global energy_pro_call
    global current_time
    global current_tick
    global needed

    # calculate how much energy is lost
    current_energy -= needed
    print(f"Needed {needed}")

    # Calculate how many calls are needed to get the energy back to the needed level
    calls = math.ceil((needed - current_energy ) / energy_pro_call)
    print(f"Calls needed: {calls}")

    # If the energy is more than the needed energy, return
    if current_energy > (needed + 1000):
        return
    
    # Generate the needed energy
    for i in range(calls):
        power(keys[random.randrange(0,KRAFTWERK_COUNT-1)])
        current_energy += energy_pro_call
    print(f"Current energy: {current_energy}")


# Main loop
while True != 0:
    # gettign the needed energy for this hour
    needed = float(HOURS_24[str(current_time % 24)])
    # tick
    tick()

    # increment the ticks
    current_tick += 1
    print(f"Time: {current_time}")
    # if the time is 24, get the needed energy for the next 24 hours, this is done every 24 hours to save calls
    if (current_time + 1) % 24 == 0:
        HOURS_24 = needed_energy()
    
    # if the tick is every sixth, get the current energy and time, this is done every hour to save calls, we are getting the time to resync the time
    if current_tick % 6 == 0:
        current_energy = get_energy()
        current_time = get_time()
    time.sleep(2)
    