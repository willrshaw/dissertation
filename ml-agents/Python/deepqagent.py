# This is the deep Q learning agent class

# imports
import numpy as np
import torch
import torch.nn.functional as functional
import torch.optim as optim
import random

from collections import namedtuple, deque


from model import DeepQNetwork


# hyper-parameters

BATCH_SIZE = 64         # size of the minibatch
BUFFER_SIZE = int(1e5)  # replay buffer size
GAMMA = 0.99            # discount factor (to aid exploration/exploitation dilemma)
TAU = 1e-3              # soft update of target params
LR = 5e-4               # learning rate, smaller number is finer but slower, bigger numbver is courser but faster
UPDATE_STEP = 4         # after how many steps to update the network


# Choosing device to run training on, try and get this to work on GPU if possible?
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")



class Agent():
    # This is the agent class
    # This will interact with the enviroment through these steps:
    # 1: Observe
    # 2: Act
    # 3: Assess rewards
    
    
    def __init__(self, state_size, action_size, seed):
        
        self.state_size = state_size
        self.action_size = action_size
        self.seed = random.seed(seed)
        
        # Deep Q Network
        self.local_dqnet = DeepQNetwork(state_size, action_size, seed).to(device)
        self.target_dqnet = DeepQNetwork(state_size, action_size, seed).to(device)
        self.optimizer = optim.Adam(self.local_dqnet.parameters(), lr = LR)
        
        # replay
        self.memory = ReplayBuffer(action_size, BUFFER_SIZE, BATCH_SIZE, seed)
        
        # time step (init here)
        self.time_step = 0

       
    # Stepping function, the enviroment moves in steps, and the agent will fo through the observe act assess process each step
    def step(self, state, action, reward, next_state, done):
        #store the experience in memory (defined as a ReplayBuffer earlier)
        self.memory.add(state, action, reward, next_state, done)
        
        # we want to learn every #UPDATE_STEP number of steps
        
        self.time_step = (self.time_step + 1) % UPDATE_STEP # modulus here to update every n steps
        if self.time_step ==0:
            if len(self.memory) > BATCH_SIZE:
                experiences = self.memory.sample() # random subset of samples used to learn
                self.learn(experiences, GAMMA)
     
    
    # Action function, this returns an action based off the current state (observations)
    def act(self, state, eps=0.):
        state = torch.from_numpy(state).float().unsqueeze(0).to(device)
        self.local_dqnet.eval()
        
        with torch.no_grad():
            action_values = self.local_dqnet(state)
        self.local_dqnet.train()

        # Epsilon-greedy action selection
        if random.random() > eps:
            return np.argmax(action_values.cpu().data.numpy())
        else:
            return random.choice(np.arange(self.action_size))
        
    # Learning function, this will select the best action from the state action pairs    
    def learn(self, experiences, gamma):
        states, actions, rewards, next_states, dones = experiences
        # get max predicted Q
        Q_targets_next = self.target_dqnet(next_states).detach().max(1)[0].unsqueeze(1)
        
        # calculate q value for current state
        Q_targets = rewards + (gamma * Q_targets_next * (1 - dones))

        # Get expected Q values from local model
        Q_expected = self.local_dqnet(states).gather(1, actions)

        # calculate los
        loss = functional.mse_loss(Q_expected, Q_targets)
        # minimise loss
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

        # target network update
        self.soft_update(self.local_dqnet, self.target_dqnet, TAU)     
        
    def soft_update(self, local_model, target_model, tau):
        for target_param, local_param in zip(target_model.parameters(), local_model.parameters()):
            target_param.data.copy_(tau*local_param.data + (1.0-tau)*target_param.data)

        
class ReplayBuffer:
    # This is how we store experience

    def __init__(self, action_size, buffer_size, batch_size, seed):
        self.action_size = action_size
        self.memory = deque(maxlen=buffer_size)  
        self.batch_size = batch_size
        self.experience = namedtuple("Experience", field_names=["state", "action", "reward", "next_state", "done"])
        self.seed = random.seed(seed)
    
    def add(self, state, action, reward, next_state, done):
        e = self.experience(state, action, reward, next_state, done)
        self.memory.append(e)
    
    def sample(self):
        experiences = random.sample(self.memory, k=self.batch_size)

        states = torch.from_numpy(np.vstack([e.state for e in experiences if e is not None])).float().to(device)
        actions = torch.from_numpy(np.vstack([e.action for e in experiences if e is not None])).long().to(device)
        rewards = torch.from_numpy(np.vstack([e.reward for e in experiences if e is not None])).float().to(device)
        next_states = torch.from_numpy(np.vstack([e.next_state for e in experiences if e is not None])).float().to(device)
        dones = torch.from_numpy(np.vstack([e.done for e in experiences if e is not None]).astype(np.uint8)).float().to(device)
  
        return (states, actions, rewards, next_states, dones)

    def __len__(self):
        return len(self.memory)
        
       
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        