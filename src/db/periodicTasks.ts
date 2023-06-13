import * as passwordResetTokenActions from "./passwordResetToken.actions";

const interval = 1000 * 60 * 5;

const deleteExpiredPasswordResetTokens = () => {
  passwordResetTokenActions.deleteExpiredTokens(Date.now() - interval);
};

export const initialize = () => {
  setInterval(() => {
    deleteExpiredPasswordResetTokens();
  }, interval);
};
