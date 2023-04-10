import {
  getGameObject,
  saveGameState,
} from '../services/gameObject.service';

const getTokenFrom = (request: any) => {
  const authorization = request.get('authorization');
  if (authorization && authorization.toLowerCase().startsWith('bearer ')) {
    return authorization.substring(7);
  }
  return null;
};

export const getGameObjectController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const getUserService = await getGameObject(token, req.params.id);
  return res.json(getUserService);
};

export const saveGameStateController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const savePlayerStateService = await saveGameState(token, req.body);
  return res.json(savePlayerStateService);
};
