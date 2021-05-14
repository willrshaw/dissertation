#This is the model class for deep learning


import torch
import torch.nn as nn
import torch.nn.functional as functional

class DeepQNetwork(nn.Module):
    
    def forward(self, current_state):
        #This a mapping of states to actions.
        
        h = functional.relu(self.hidden_layer1(current_state))
        h = functional.relu(self.hidden_layer2(h))
        return self.hidden_layer3(h)
        
        

    def __init__(self, state_size, actions_size, seed, hidden_layer1_size=64, hidden_layer2_size=64):
        
        
        super(DeepQNetwork, self).__init__()
        self.seed = torch.manual_seed(seed)
        self.hidden_layer1 = nn.Linear(state_size, hidden_layer1_size)
        self.hidden_layer2 = nn.Linear(hidden_layer1_size, hidden_layer2_size)
        self.hidden_layer3 = nn.Linear(hidden_layer2_size, actions_size)
        