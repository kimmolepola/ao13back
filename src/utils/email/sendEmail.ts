import nodemailer from 'nodemailer';
import handlebars from 'handlebars';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// eslint-disable-next-line consistent-return
const sendEmail = async (email: any, subject: any, payload: any, template: any) => {
  console.log('send email');
  try {
    // create reusable transporter object using the default SMTP transport
    const transporter = nodemailer.createTransport({
      host: process.env.EMAIL_HOST,
      port: 465,
      auth: {
        user: process.env.EMAIL_USERNAME,
        pass: process.env.APP_PASSWORD,
      },
    });
    const filename = fileURLToPath(import.meta.url);
    const dirname = path.dirname(filename);
    const source = fs.readFileSync(path.join(dirname, template), 'utf8');
    const compiledTemplate = handlebars.compile(source);
    const options = () => ({
      from: process.env.FROM_EMAIL,
      to: email,
      subject,
      html: compiledTemplate(payload),
    });

    // Send email
    return transporter.sendMail(options());
  } catch (error) {
    console.log('utils -> email -> sendEmail error:', error);
    return false;
  }
};

export default sendEmail;
