const signupRequests: {
  [email: string]: { timestamp: number; token: string };
} = {};

const deleteExpiredSignupRequests = (timeToLive: number) => {
  const expirationTime = Date.now() - timeToLive;
  const keys = Object.keys(signupRequests);
  for (const key of keys) {
    if (signupRequests[key].timestamp < expirationTime) {
      delete signupRequests[key];
    }
  }
};

export const get = (email: string) => signupRequests[email];
export const set = (email: string, token: string) => {
  signupRequests[email] = { token, timestamp: Date.now() };
};
export const remove = (email: string) => {
  delete signupRequests[email];
};
export const initialize = () => {
  const interval = 1000 * 60 * 5;
  setInterval(() => deleteExpiredSignupRequests(interval), interval);
};
