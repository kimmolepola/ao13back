let main: any;
const clients: any = {};

export const addClientUnique = (id: any, socket: any) => {
  console.log('--add unique, id:', id, 'clients:', Object.keys(clients));
  if (clients[id]) {
    return false;
  }
  clients[id] = socket;
  return true;
};

export const removeClient = (id: any) => {
  delete clients[id];
};

export const getClients = () => clients;

export const setMain = (x: any) => {
  console.log('--set main:', x);
  main = x;
};

export const getMain = () => main;
