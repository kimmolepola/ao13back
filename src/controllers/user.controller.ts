import { getTokenFrom } from './auth.controller';
import {
  checkOkToStart,
  getUser,
  updateUsername,
} from '../services/user.service';

export const checkOkToStartController = (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const checkOkToStartService = checkOkToStart(token);
  return res.json(checkOkToStartService);
};

export const getUserController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const getUserService = await getUser(token);
  return res.json(getUserService);
};

export const updateUsernameController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const updateUsernameService = await updateUsername(token, req.body);
  return res.json(updateUsernameService);
};
